using Newtonsoft.Json.Bson;
using UnityEngine;

public class PlayerPaint : MonoBehaviour
{
    public SpriteRenderer head;
    public SpriteRenderer body;
    public SpriteRenderer leftArm;
    public SpriteRenderer rightArm;
    public SpriteRenderer leftLeg;
    public SpriteRenderer rightLeg;

    public FlexibleColorPicker FlexibleColorPicker;

    private SpriteRenderer selectedBodyType;

    public void PaintPlayer()
    {
        selectedBodyType.color = FlexibleColorPicker.GetColor();
    }
    public void SelectBodyHead()
    {
        selectedBodyType = head;
    }
    public void SelectBodyBody()
    {
        selectedBodyType = body;
    }
    public void SelectBodyLeftArm()
    {
        selectedBodyType = leftArm;
    }
    public void SelectBodyRightArm()
    {
        selectedBodyType = rightArm;
    }
    public void SelectBodyLeftLeg()
    {
        selectedBodyType = leftLeg;
    }
    public void SelectBodyRightLeg()
    {
        selectedBodyType = rightLeg;
    }
}
