using UnityEngine;

public class BookStateMachine : StateMachineBehaviour
{
    private bool hasPlayedAnimation = false;

    // Animation names
    private const string ANIM_OPEN = "book-opening";
    private const string ANIM_OPEN_IDLE = "book-openidle";

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[BookStateMachine] Entered state: {stateInfo.shortNameHash}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Check if we're in the opening animation and it's almost done
        if (stateInfo.IsName(ANIM_OPEN) && stateInfo.normalizedTime >= 0.9f && !hasPlayedAnimation)
        {
            Debug.Log("[BookStateMachine] Opening animation almost complete, transitioning to open idle");
            animator.Play(ANIM_OPEN_IDLE);
            hasPlayedAnimation = true;
        }
    }
} 