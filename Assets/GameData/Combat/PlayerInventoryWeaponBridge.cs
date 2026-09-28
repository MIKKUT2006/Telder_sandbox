using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Game.Combat
{
    public sealed class PlayerInventoryWeaponBridge :
        MonoBehaviour
    {
        [SerializeField]
        private MonoBehaviour inventoryComponent;

        private Type inventoryType;
        private MethodInfo getSelectedItemId;
        private MethodInfo getSelectedStack;
        private MethodInfo hasItem;
        private MethodInfo removeItem;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            if (inventoryComponent == null)
                inventoryComponent = FindInventory();

            if (inventoryComponent == null)
            {
                Debug.LogError(
                    "WEAPON: PlayerInventory not found."
                );

                return;
            }

            inventoryType =
                inventoryComponent.GetType();

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            getSelectedItemId =
                inventoryType.GetMethod(
                    "GetSelectedItemId",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null
                );

            getSelectedStack =
                inventoryType.GetMethod(
                    "GetSelectedStack",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null
                );

            hasItem =
                inventoryType
                    .GetMethods(flags)
                    .FirstOrDefault(
                        m =>
                            m.Name ==
                                "HasItem" &&
                            m.GetParameters()
                                .Length ==
                                2
                    );

            removeItem =
                inventoryType
                    .GetMethods(flags)
                    .FirstOrDefault(
                        m =>
                            m.Name ==
                                "RemoveItem" &&
                            m.GetParameters()
                                .Length ==
                                2
                    );
        }

        public string GetSelectedItemId()
        {
            Initialize();

            if (inventoryComponent == null)
                return null;

            if (getSelectedItemId != null)
            {
                object value =
                    getSelectedItemId.Invoke(
                        inventoryComponent,
                        null
                    );

                return IdToString(value);
            }

            if (getSelectedStack != null)
            {
                object stack =
                    getSelectedStack.Invoke(
                        inventoryComponent,
                        null
                    );

                if (stack == null)
                    return null;

                Type type =
                    stack.GetType();

                object id =
                    ReadMember(
                        stack,
                        type,
                        "ItemId"
                    ) ??
                    ReadMember(
                        stack,
                        type,
                        "ID"
                    ) ??
                    ReadMember(
                        stack,
                        type,
                        "Id"
                    );

                return IdToString(id);
            }

            return null;
        }

        public bool HasItem(
            string itemId,
            int amount)
        {
            Initialize();

            if (amount <= 0 ||
                string.IsNullOrWhiteSpace(
                    itemId))
            {
                return true;
            }

            if (inventoryComponent == null ||
                hasItem == null)
            {
                return false;
            }

            object result =
                InvokeItemAmountMethod(
                    hasItem,
                    itemId,
                    amount
                );

            return result is bool b
                ? b
                : result != null;
        }

        public bool RemoveItem(
            string itemId,
            int amount)
        {
            Initialize();

            if (amount <= 0 ||
                string.IsNullOrWhiteSpace(
                    itemId))
            {
                return true;
            }

            if (!HasItem(
                    itemId,
                    amount))
            {
                return false;
            }

            if (inventoryComponent == null ||
                removeItem == null)
            {
                return false;
            }

            object result =
                InvokeItemAmountMethod(
                    removeItem,
                    itemId,
                    amount
                );

            if (removeItem.ReturnType ==
                typeof(void))
            {
                return true;
            }

            return result is bool b
                ? b
                : result != null;
        }

        private object InvokeItemAmountMethod(
            MethodInfo method,
            string itemId,
            int amount)
        {
            ParameterInfo[] p =
                method.GetParameters();

            object id =
                ConvertId(
                    itemId,
                    p[0].ParameterType
                );

            object count =
                Convert.ChangeType(
                    amount,
                    p[1].ParameterType
                );

            return method.Invoke(
                inventoryComponent,
                new[]
                {
                    id,
                    count
                }
            );
        }

        private MonoBehaviour FindInventory()
        {
            MonoBehaviour[] all =
                GetComponentsInParent<
                    MonoBehaviour
                >(
                    true
                )
                .Concat(
                    GetComponentsInChildren<
                        MonoBehaviour
                    >(
                        true
                    )
                )
                .ToArray();

            for (int i = 0;
                 i < all.Length;
                 i++)
            {
                MonoBehaviour b =
                    all[i];

                if (b != null &&
                    b.GetType().Name ==
                    "PlayerInventory")
                {
                    return b;
                }
            }

            return null;
        }

        private static string IdToString(
            object value)
        {
            if (value == null)
                return null;

            string result =
                value.ToString();

            return string.IsNullOrWhiteSpace(
                    result)
                ? null
                : result.Trim();
        }

        private static object ConvertId(
            string id,
            Type targetType)
        {
            if (targetType ==
                typeof(string))
            {
                return id;
            }

            MethodInfo parse =
                targetType.GetMethod(
                    "Parse",
                    BindingFlags.Static |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(string)
                    },
                    null
                );

            if (parse != null)
            {
                return parse.Invoke(
                    null,
                    new object[]
                    {
                        id
                    }
                );
            }

            ConstructorInfo ctor =
                targetType.GetConstructor(
                    new[]
                    {
                        typeof(string)
                    }
                );

            if (ctor != null)
            {
                return ctor.Invoke(
                    new object[]
                    {
                        id
                    }
                );
            }

            return id;
        }

        private static object ReadMember(
            object instance,
            Type type,
            string name)
        {
            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            FieldInfo field =
                type.GetField(
                    name,
                    flags
                );

            if (field != null)
                return field.GetValue(instance);

            PropertyInfo property =
                type.GetProperty(
                    name,
                    flags
                );

            if (property != null &&
                property.CanRead)
            {
                return property.GetValue(
                    instance
                );
            }

            return null;
        }
    }
}
