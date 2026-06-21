using UnityEngine;

public class BuildAreaHover : MonoBehaviour
{
    [SerializeField] private BuilderController builder;
    [SerializeField] private Animator zoneAnimator;

    public void OnPointerEnter()
    {
        Debug.Log("ENTER");

        builder.SetHoveringBuildArea(true);

        if (zoneAnimator != null)
        {
            zoneAnimator.SetBool("Hovering", true);

            Debug.Log("Animator Hovering = " +
                    zoneAnimator.GetBool("Hovering"));
        }
    }

    public void OnPointerExit()
    {
        Debug.Log("EXIT");

        builder.SetHoveringBuildArea(false);

        if (zoneAnimator != null)
            zoneAnimator.SetBool("Hovering", false);
    }
}