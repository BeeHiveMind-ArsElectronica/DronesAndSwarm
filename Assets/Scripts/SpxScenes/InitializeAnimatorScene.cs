using UnityEngine;

public class InitializeAnimatorScene : MonoBehaviour
{
    [SerializeField]
    private string m_animationControllerName;

    public void InitializeAnimator()
    {
        AnimatorSceneManager asm = GetComponent<AnimatorSceneManager>();

        if (asm)
        {
            string controllerName = Util.RemoveSpecialCharacters(m_animationControllerName);
            asm.InitializeAnimator(true, controllerName);
        }
        else
        {
            Debug.LogError("AnimatorSceneManager not found!");
        }
    }
}
