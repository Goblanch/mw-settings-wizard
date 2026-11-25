using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiradorWorks.Settings.Samples
{
    /// <summary>
    /// "Keep these changes?" with a countdown. Hides itsef if the request completes.
    /// </summary>
    public sealed class ConfirmationDialog : MonoBehaviour
    {
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Button keepButton;
        [SerializeField] Button revertButton;
        [Tooltip("{0} is replaced by the seconds left")]
        [SerializeField] string countdownFormat = "Reverting in {0} s";

        ConfirmationRequest request;

        void Awake()
        {
            keepButton.onClick.AddListener(() => request?.Confirm());
            revertButton.onClick.AddListener(() => request.Cancel());

            if (request == null)
                gameObject.SetActive(false);
        }

        public void Show(ConfirmationRequest confirmation)
        {
            if (confirmation == null || confirmation.IsCompleted)
                return;

            if (request != null)
                request.Completed -= OnCompleted;

            request = confirmation;
            request.Completed += OnCompleted;

            gameObject.SetActive(true);
            UpdateCountdown();
        }

        void Update() => UpdateCountdown();

        void OnDestroy()
        {
            if (request != null)
                request.Completed -= OnCompleted;
        }

        void UpdateCountdown()
        {
            if (request != null && countdownText != null)
                countdownText.text = string.Format(countdownFormat, Mathf.CeilToInt(request.RemainingSeconds));
        }

        void OnCompleted(bool confirmed)
        {
            request = null;
            gameObject.SetActive(false);
        }
    }
}