using System.Collections;
using Game.Content;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.SpecialWorlds
{
    /// <summary>
    /// Standalone challenge scene. It deliberately does not start WorldManager,
    /// EntitySpawnManager, SaveGameRuntime, or the normal survival inventory.
    /// </summary>
    public sealed class SpecialHarpoonChallenge : MonoBehaviour
    {
        private enum HookState { Ready, Flying, Attached }
        private static bool launching;
        private HarpoonCaveGenerator caves;
        private Rigidbody2D body;
        private DistanceJoint2D ropeJoint;
        private Transform actor;
        private HarpoonChallengeAvatar avatar;
        private HarpoonRunDirector director;
        private Transform attachedSurface;
        private Vector3 attachedLocalPoint;
        private Transform heldWeapon;
        private Transform hookVisual;
        private Camera view;
        private LineRenderer ropeRenderer;
        private HookState hookState;
        private Vector2 hookPoint;
        private Vector2 hookVelocity;
        private float flightTime;
        private float ropeLength;
        private float startX;
        private float maxX;
        private int bestDistance;
        private bool paused;
        private float moveAxis;
        private Vector2 touchMovement;
        private Vector2 touchAim;
        private bool touchFireRequested;
        private bool touchJumpRequested;
        private bool touchReleaseRequested;
        private bool isMobile;
        private float nextFireTime;
        private float gameStartTime;
        private float runDuration;
        private GUIStyle hudStyle;
        private GUIStyle centerStyle;
        private bool loaded;
        private bool initialized;
        private string initializationError;
        private bool warnedCameraRestored;

        // Camera is created synchronously, before the menu scene is unloaded.
        // Waiting until MonoBehaviour.Start() previously allowed the runtime
        // scene to remain without any rendering camera when initialization failed.
        public static void Launch()
        {
            if (launching) return;
            launching = true;
            Scene old = SceneManager.GetActiveScene();
            Scene challenge = default(Scene);
            try
            {
                challenge = SceneManager.CreateScene("Telder_SpecialWorld_Harpoon");
                if (!SceneManager.SetActiveScene(challenge))
                    throw new System.InvalidOperationException("Could not activate the harpoon scene.");

                var controllerGO = new GameObject("SpecialWorld_Harpoon_Controller");
                if (controllerGO.scene != challenge)
                    SceneManager.MoveGameObjectToScene(controllerGO, challenge);
                var runtime = controllerGO.AddComponent<SpecialHarpoonChallenge>();

                // Force the camera into our new scene, not the MainMenu scene.
                // Create this BEFORE generating caves, colliders and textures.
                runtime.EnsureChallengeCamera();
                runtime.InitializeChallenge();
                runtime.StartCoroutine(runtime.UnloadOldScene(old));
                Debug.Log("SPECIAL HARPOON: scene launched, camera=" +
                    (runtime.view != null ? runtime.view.gameObject.scene.name : "NULL") +
                    ", initialized=" + runtime.loaded);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                launching = false;
                if (old.IsValid() && old.isLoaded)
                    SceneManager.SetActiveScene(old);
                if (challenge.IsValid() && challenge.isLoaded && challenge != old)
                    SceneManager.UnloadSceneAsync(challenge);
            }
        }

        private IEnumerator UnloadOldScene(Scene old)
        {
            // Even if world generation failed, keep the challenge camera alive
            // so its on-screen error / exit button remains visible.
            yield return null;
            EnsureChallengeCamera();
            if (old.IsValid() && old.isLoaded && old != gameObject.scene)
            {
                AsyncOperation unloading = SceneManager.UnloadSceneAsync(old);
                if (unloading != null) yield return unloading;
            }
            EnsureChallengeCamera();
        }

        private void Start()
        {
            // Safe fallback if the component was created by a scene or prefab.
            // Launch() initializes synchronously and does not initialize twice.
            InitializeChallenge();
        }

        private void InitializeChallenge()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                EnsureChallengeCamera();
                Time.timeScale = 1f;
                bestDistance = PlayerPrefs.GetInt("telder.special.harpoon.best", 0);
                gameStartTime = Time.time;
                isMobile = Application.isMobilePlatform;
                BuildWorld();
                RestartRun();
                if (isMobile) CreateTouchControls();
                loaded = true;
                initializationError = null;
            }
            catch (System.Exception e)
            {
                initializationError = e.GetType().Name + ": " + e.Message;
                Debug.LogException(e, this);
                loaded = false;
                // Do not remove the camera on initialization failure.
                EnsureChallengeCamera();
            }
        }

        private void EnsureChallengeCamera()
        {
            Scene ownerScene = gameObject.scene;
            if (view == null || view.gameObject.scene != ownerScene)
            {
                var go = new GameObject("HarpoonChallengeCamera");
                if (ownerScene.IsValid() && go.scene != ownerScene)
                    SceneManager.MoveGameObjectToScene(go, ownerScene);
                view = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                Debug.Log("SPECIAL HARPOON: Created camera in scene '" + go.scene.name + "'");
            }

            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
            view.enabled = true;
            view.tag = "MainCamera";
            view.orthographic = true;
            view.orthographicSize = 8.2f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.31f, .41f, .57f, 1f);
            view.cullingMask = ~0;
            view.targetTexture = null;
            view.rect = new Rect(0f, 0f, 1f, 1f);
            view.depth = 100f;
            if (view.transform.position.z > -1f)
                view.transform.position = new Vector3(5f, 2f, -20f);

            // When URP is installed it may require its camera-specific data.
            // Use optional reflection so this file also compiles in Built-in RP.
            System.Type urpCameraData = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCameraData != null && view.GetComponent(urpCameraData) == null)
            {
                try { view.gameObject.AddComponent(urpCameraData); }
                catch (System.Exception e)
                {
                    Debug.LogWarning("SPECIAL HARPOON: Optional URP camera setup failed: " + e.Message);
                }
            }
        }

        private void LateUpdate()
        {
            // Other persistent menu/game systems may disable cameras during
            // scene transitions. Restore the one owned by this challenge.
            if (view == null || !view.enabled || !view.gameObject.activeInHierarchy)
            {
                EnsureChallengeCamera();
                if (!warnedCameraRestored)
                {
                    warnedCameraRestored = true;
                    Debug.LogWarning("SPECIAL HARPOON: Challenge camera was missing/disabled and was restored.", this);
                }
            }
            if(avatar!=null)avatar.EnsureVisible();
        }

        private GameObject NewChallengeObject(string name)
        {
            var go = new GameObject(name);
            if (gameObject.scene.IsValid() && go.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            return go;
        }

        private void BuildWorld()
        {
            var cave = NewChallengeObject("Procedural_Bright_Caves");
            caves = cave.AddComponent<HarpoonCaveGenerator>();
            EnsureChallengeCamera();
            view.transform.position = new Vector3(5f, 2f, -20f);
            var player = NewChallengeObject("SpecialHarpoonPlayer");
            actor = player.transform;
            body = player.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 2.15f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.freezeRotation = true;
            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.72f, 1.45f);
            collider.direction = CapsuleDirection2D.Vertical;
            ropeJoint = player.AddComponent<DistanceJoint2D>();
            ropeJoint.enabled = false;
            ropeJoint.autoConfigureDistance = false;
            ropeJoint.autoConfigureConnectedAnchor = false;
            ropeJoint.enableCollision = true;
            ropeJoint.anchor = Vector2.up * .2f;
            var visualRoot=NewChallengeObject("Harpoon avatar");visualRoot.transform.SetParent(actor,false);
            avatar=visualRoot.AddComponent<HarpoonChallengeAvatar>();avatar.Initialize();
            director=gameObject.AddComponent<HarpoonRunDirector>();director.Initialize(caves);
            heldWeapon = Visual("HarpoonHeld", "items/harpoon", actor,
                new Vector3(.45f, .01f), .72f, 110);
            var ropeGO = NewChallengeObject("HarpoonRope");
            ropeRenderer = ropeGO.AddComponent<LineRenderer>();
            ropeRenderer.positionCount = 2;
            ropeRenderer.startWidth = .055f;
            ropeRenderer.endWidth = .055f;
            ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            ropeRenderer.startColor = new Color(.82f, .59f, .40f);
            ropeRenderer.endColor = new Color(.93f, .79f, .60f);
            ropeRenderer.sortingOrder = 45;
            ropeRenderer.enabled = false;
            hookVisual = NewChallengeObject("HarpoonHook").transform;
            var hookSprite = hookVisual.gameObject.AddComponent<SpriteRenderer>();
            hookSprite.sprite = UnityEngine.Resources.Load<Sprite>("Harpoon/harpoon_hook");
            if (hookSprite.sprite == null) hookSprite.sprite = caves.GetSprite("items/harpoon_deployed");
            hookSprite.sortingOrder = 46;
            hookVisual.localScale = Vector3.one * .65f;
            hookVisual.gameObject.SetActive(false);
        }

        private Transform Visual(string objectName, string texture, Transform parent,
            Vector3 localPosition, float scale, int sorting)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = caves.GetSprite(texture);
            sr.sortingOrder = sorting;
            return go.transform;
        }

        private void RestartRun()
        {
            if (body == null || caves == null) return;
            paused = false;
            Time.timeScale = 1f;
            Disconnect(false);
            caves.ResetCourse(Random.Range(10000, 99999999));
            director.ResetRun(caves.Seed);
            body.gravityScale=2.15f;
            nextFireTime=0;moveAxis=0;touchMovement=touchAim=Vector2.zero;
            touchFireRequested=touchJumpRequested=touchReleaseRequested=false;stickWasTriggered=false;
            body.position = new Vector2(0f, -2.25f);
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            maxX = startX = 0f;
            gameStartTime = Time.time;
            if (view != null) view.transform.position = new Vector3(5f, 2f, -20f);
        }

        private void Update()
        {
            if (!loaded || body == null || caves == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.R)) RestartRun();
            Time.timeScale = paused ? 0f : 1f;
            if (paused) return;
            caves.Stream(body.position.x);
            if(director.Tick(Time.deltaTime,body)){RestartRun();return;}
            runDuration=director.Rules.Elapsed;
            body.gravityScale=2.15f*director.Rules.Gravity;
            maxX = Mathf.Max(maxX, body.position.x);
            int distance = Mathf.FloorToInt(maxX - startX);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                if (bestDistance % 10 == 0)
                {
                    PlayerPrefs.SetInt("telder.special.harpoon.best", bestDistance);
                    PlayerPrefs.Save();
                }
            }
            if (body.position.y < -17f)
            {
                RestartRun();
                return;
            }
            moveAxis = Mathf.Clamp(InputAxis() + touchMovement.x, -1f, 1f);
            if (Input.GetKeyDown(KeyCode.Space) || touchJumpRequested)
            {
                touchJumpRequested = false;
                if (hookState == HookState.Attached)
                {
                    Disconnect(true);
                }
                else if (Grounded())
                {
                    Vector2 v = body.velocity; v.y = 0; body.velocity = v;
                    body.AddForce(Vector2.up * 12f, ForceMode2D.Impulse);
                }
            }
            if (Input.GetMouseButtonDown(0) && !isMobile && !PointerOnUI()) FireAt(GetMouseWorld());
            if (Input.GetMouseButtonDown(1) && !isMobile) Disconnect(false);
            if (touchFireRequested)
            {
                touchFireRequested = false;
                FireAt((Vector2)actor.position + touchAim * 18f);
            }
            if (touchReleaseRequested)
            {
                touchReleaseRequested = false;
                Disconnect(false);
            }
            if (hookState == HookState.Attached)
            {
                float reel = (Input.GetKey(KeyCode.W) ? 1f : 0f) -
                             (Input.GetKey(KeyCode.S) ? 1f : 0f);
                reel += Input.mouseScrollDelta.y;
                // An expiring winch does not abruptly shorten an attached rope.
                float maxLength=Mathf.Max(24f*director.Rules.Grapple,ropeLength);
                ropeLength = Mathf.Clamp(ropeLength - reel * Time.deltaTime * 6f * director.Rules.Grapple, 2f, maxLength);
                ropeJoint.distance = ropeLength;
            }
            AnimateGrapple(Time.deltaTime);
            if (heldWeapon != null)
            {
                Vector2 aim = isMobile && touchAim.sqrMagnitude > .03f
                    ? touchAim : (GetMouseWorld() - (Vector2)actor.position).normalized;
                avatar.Animate(body.velocity,hookState==HookState.Attached,aim);
                heldWeapon.localPosition=new Vector3(aim.x>=0?.45f:-.45f,.01f,0);
                var heldSprite=heldWeapon.GetComponent<SpriteRenderer>();if(heldSprite!=null)heldSprite.flipY=aim.x<0;
                heldWeapon.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 30f);
            }
            Vector3 cameraTarget = new Vector3(Mathf.Max(5f, body.position.x + 5f),
                Mathf.Clamp(body.position.y + 2.1f, -1.5f, 6f), -20f);
            view.transform.position = Vector3.Lerp(view.transform.position,
                cameraTarget, Mathf.Clamp01(Time.deltaTime * 4.5f));
        }

        private float InputAxis()
        {
            return (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                   (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        }

        private bool PointerOnUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool Grounded()
        {
            Vector2 at = (Vector2)actor.position + Vector2.down * .77f;
            var hit = Physics2D.Raycast(at, Vector2.down, .22f);
            return hit.collider != null && hit.rigidbody != body;
        }

        private void FixedUpdate()
        {
            if(!loaded||paused||body==null)return;
            float speed=director.Rules.Speed;
            float wind=director.Rules.Modifier==HarpoonModifier.Tailwind?2.5f:0;
            if(hookState==HookState.Attached)
            {
                body.AddForce(new Vector2(moveAxis*47f*speed+wind*8,0),ForceMode2D.Force);
                body.velocity=Vector2.ClampMagnitude(body.velocity,22f*speed);
            }
            else
            {
                Vector2 velocity=body.velocity;
                velocity.x=Mathf.MoveTowards(velocity.x,moveAxis*9f*speed+wind,40f*Time.fixedDeltaTime);
                body.velocity=velocity;
            }
        }

        private void FireAt(Vector2 target)
        {
            if (Time.time < nextFireTime || body == null) return;
            if (hookState != HookState.Ready)
            {
                Disconnect(false);
                nextFireTime = Time.time + .15f;
                return;
            }
            Vector2 from = (Vector2)actor.position + new Vector2(.25f, .15f);
            Vector2 delta = target - from;
            if (delta.sqrMagnitude < .3f) return;
            hookState = HookState.Flying;
            hookPoint = from;
            hookVelocity = (delta.normalized + Vector2.up * .17f).normalized * 30f * director.Rules.Grapple;
            flightTime = 0f;
            hookVisual.gameObject.SetActive(true);
            ropeRenderer.enabled = true;
            nextFireTime = Time.time + .23f;
        }

        private void AnimateGrapple(float dt)
        {
            if (hookState == HookState.Ready)
            {
                ropeRenderer.enabled = false;
                hookVisual.gameObject.SetActive(false);
                return;
            }
            if (hookState == HookState.Flying)
            {
                Vector2 movement = hookVelocity * dt;
                RaycastHit2D hit = Physics2D.Raycast(hookPoint,
                    movement.normalized, movement.magnitude);
                flightTime += dt;
                if (hit.collider != null && hit.rigidbody != body)
                {
                    hookPoint = hit.point;
                    attachedSurface=hit.collider.transform;
                    attachedLocalPoint=attachedSurface.InverseTransformPoint(hit.point);
                    hookState = HookState.Attached;
                    ropeLength = Mathf.Clamp(Vector2.Distance(actor.position, hookPoint), 2f, 23f*director.Rules.Grapple);
                    ropeJoint.connectedAnchor = hookPoint;
                    ropeJoint.distance = ropeLength;
                    ropeJoint.enabled = true;
                }
                else hookPoint += movement;
                hookVelocity += Vector2.down * (14f * dt);
                if (flightTime > 1.8f || Vector2.Distance(hookPoint, actor.position) > 24f*director.Rules.Grapple)
                {
                    Disconnect(false);
                    return;
                }
            }
            if(hookState==HookState.Attached)
            {
                if(attachedSurface==null){Disconnect(false);return;}
                hookPoint=attachedSurface.TransformPoint(attachedLocalPoint);
                ropeJoint.connectedAnchor=hookPoint;
            }
            Vector2 hand = (Vector2)actor.position + new Vector2(.23f, .15f);
            ropeRenderer.SetPosition(0, hand);
            ropeRenderer.SetPosition(1, hookPoint);
            hookVisual.position = hookPoint;
            if (hookState == HookState.Flying)
                hookVisual.rotation = Quaternion.Euler(0, 0,
                    Mathf.Atan2(hookVelocity.y, hookVelocity.x) * Mathf.Rad2Deg);
        }

        private void Disconnect(bool jump)
        {
            hookState = HookState.Ready;
            attachedSurface=null;
            if (ropeJoint != null) ropeJoint.enabled = false;
            if (ropeRenderer != null) ropeRenderer.enabled = false;
            if (hookVisual != null) hookVisual.gameObject.SetActive(false);
            if (jump && body != null)
            {
                Vector2 v = body.velocity;
                body.velocity = new Vector2(v.x, Mathf.Max(0f, v.y));
                body.AddForce(new Vector2(moveAxis * 2.5f, 9f), ForceMode2D.Impulse);
            }
        }

        private Vector2 GetMouseWorld()
        {
            if (view == null) return actor != null ? actor.position : Vector3.zero;
            Vector3 pixel = Input.mousePosition;
            pixel.z = 20f;
            return view.ScreenToWorldPoint(pixel);
        }

        private static string L(string key, string source)
        {
            return GameLocalization.Get(key, source);
        }

        private void OnGUI()
        {
            GUI.depth = -160;
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label);
                hudStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * .028f, 16f, 29f));
                hudStyle.fontStyle = FontStyle.Bold;
                hudStyle.wordWrap = true;
                hudStyle.normal.textColor = Color.white;
                centerStyle = new GUIStyle(hudStyle);
                centerStyle.alignment = TextAnchor.MiddleCenter;
            }
            if (!loaded)
            {
                GUI.Label(new Rect(40, 30, Screen.width - 80, 100),
                    L("ui.special.error", "Не удалось создать испытание. Проверь Unity Console / Android Logcat."), centerStyle);
                if (!string.IsNullOrEmpty(initializationError))
                    GUI.Label(new Rect(40, 115, Screen.width - 80, 80), initializationError, centerStyle);
                if (GUI.Button(new Rect(Screen.width / 2f - 120, 205, 240, 65), L("ui.special.menu", "Главное меню"))) ReturnToMenu();
                return;
            }
            if (!paused)
            {
                GUI.Label(new Rect(20, 12, Screen.width * .63f, 42),
                    L("ui.special.hud", "ГАРПУН") + "    " + Mathf.FloorToInt(maxX) + " m    •    " + L("ui.special.record", "Рекорд") + " " + bestDistance + " m", hudStyle);
                if (!isMobile)
                    GUI.Label(new Rect(20, 53, Screen.width - 20, 36),
                        L("ui.special.help", "A/D — движение  •  ЛКМ — гарпун/отцепиться  •  Space — прыжок  •  W/S — верёвка  •  R — новый маршрут"), hudStyle);
                GUI.Label(new Rect(20,isMobile?56:94,Screen.width-40,70),director.Status(),hudStyle);
                if(!string.IsNullOrEmpty(director.Notice))GUI.Label(new Rect(20,isMobile?130:165,Screen.width-40,45),director.Notice,hudStyle);
                if (GUI.Button(new Rect(Screen.width - 138, 10, 128, 47), L("ui.special.pause", "II  ПАУЗА"))) paused = true;
            }
            else
            {
                Rect panel = new Rect(Screen.width * .32f, Screen.height * .27f,
                    Screen.width * .36f, Screen.height * .47f);
                GUI.Box(panel, L("ui.special.pause_title", "ГАРПУН — ПАУЗА"));
                float x = panel.x + panel.width * .12f;
                float w = panel.width * .76f;
                if (GUI.Button(new Rect(x, panel.y + 65, w, 55), L("ui.special.continue", "Продолжить"))) paused = false;
                if (GUI.Button(new Rect(x, panel.y + 133, w, 55), L("ui.special.restart", "Новая пещера"))) RestartRun();
                if (GUI.Button(new Rect(x, panel.y + 201, w, 55), L("ui.special.exit", "Особые миры / Меню"))) ReturnToMenu();
            }
        }

        private void ReturnToMenu()
        {
            PlayerPrefs.SetInt("telder.special.harpoon.best", bestDistance);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            launching = false;
            SpecialWorldsMenu.ReturnToSelectorAfterSceneLoad();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            launching = false;
            Time.timeScale = 1f;
            if (ropeRenderer != null && ropeRenderer.material != null) Destroy(ropeRenderer.material);
        }

        // The touch UI is created only for this scene; it does not depend on
        // MobileGameplayControls, which is intentionally limited to Game.
        private void CreateTouchControls()
        {
            var events = EventSystem.current;
            if (events == null)
            {
                var e = new GameObject("HarpoonEventSystem");
                events = e.AddComponent<EventSystem>();
                e.AddComponent<StandaloneInputModule>();
            }
            var go = new GameObject("HarpoonTouchHUD", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            CreateStick(go.transform, "MOVE", new Vector2(0f, 0f), new Vector2(200, 170), false);
            CreateStick(go.transform, "HOOK", new Vector2(1f, 0f), new Vector2(-200, 170), true);
            CreateTouchButton(go.transform, L("ui.special.jump", "ПРЫЖОК"), new Vector2(1f, 0f), new Vector2(-380, 140),
                () => touchJumpRequested = true);
            CreateTouchButton(go.transform, L("ui.special.release", "ОТПУСТИТЬ"), new Vector2(1f, 0f), new Vector2(-200, 350),
                () => touchReleaseRequested = true);
        }

        private void CreateStick(Transform root, string name, Vector2 anchor, Vector2 location, bool aim)
        {
            var go = new GameObject(name + "Joystick", typeof(RectTransform), typeof(Image),
                typeof(SpecialHarpoonTouchStick));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = location;
            rect.sizeDelta = new Vector2(210, 210);
            go.GetComponent<Image>().color = new Color(.74f, .82f, .94f, .25f);
            var stick = go.GetComponent<SpecialHarpoonTouchStick>();
            stick.Initialize(aim ? (System.Action<Vector2>)(v =>
            {
                touchAim = v;
                if (v.sqrMagnitude > .32f && !stickWasTriggered)
                {
                    stickWasTriggered = true;
                    touchFireRequested = true;
                }
                if (v.sqrMagnitude < .04f) stickWasTriggered = false;
            }) : (v => touchMovement = v));
            var title = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
            title.transform.SetParent(go.transform, false);
            var t = title.GetComponent<TextMeshProUGUI>();
            t.text = name; t.fontSize = 29; t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white; t.raycastTarget = false;
            var tr = (RectTransform)title.transform;
            tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        }
        private bool stickWasTriggered;

        private void CreateTouchButton(Transform root, string caption, Vector2 anchor,
            Vector2 location, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = location;
            r.sizeDelta = new Vector2(180, 90);
            var img = go.GetComponent<Image>();
            img.color = new Color(.23f, .37f, .55f, .72f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = caption; label.fontSize = 21;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var textRect = (RectTransform)labelGO.transform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        }
    }
}
