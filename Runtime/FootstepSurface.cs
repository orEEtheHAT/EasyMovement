using EasyFeedbacks;
using UnityEngine;

namespace EasyFootsteps
{
    [DisallowMultipleComponent]
    public sealed class FootstepSurface : MonoBehaviour
    {
        [SerializeField] private FeedbackPreset feedback;
        public FeedbackPreset Feedback => feedback;
    }
}
