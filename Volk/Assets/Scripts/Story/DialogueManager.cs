using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.Story
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        public Image speakerPortrait;
        public Image dialogueBox;
        public CanvasGroup canvasGroup;
        public GameObject tapPrompt;

        [Header("Settings")]
        public float typeSpeed = 0.03f;
        public float fadeSpeed = 0.5f;

        [Header("Portrait Positions")]
        public RectTransform leftPortraitSlot;
        public RectTransform rightPortraitSlot;

        private DialogueEntry[] currentDialogue;
        private int currentLineIndex;
        private bool isTyping;
        private bool skipRequested;
        private bool isIntro;
        private Coroutine typeCoroutine;

        void Start()
        {
            if (StoryManager.Instance == null || StoryManager.Instance.CurrentChapter == null)
            {
                Debug.LogWarning("[Dialogue] No story context, returning to MainMenu");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                return;
            }

            var chapter = StoryManager.Instance.CurrentChapter;
            isIntro = !StoryManager.Instance.ShowOutroDialogue;

            if (isIntro && chapter.introDialogue != null && chapter.introDialogue.Length > 0)
            {
                currentDialogue = chapter.introDialogue;
            }
            else if (!isIntro && chapter.outroDialogue != null && chapter.outroDialogue.Length > 0)
            {
                currentDialogue = chapter.outroDialogue;
            }

            if (currentDialogue == null || currentDialogue.Length == 0)
            {
                OnDialogueComplete();
                return;
            }

            currentLineIndex = 0;
            StartCoroutine(FadeIn());
            ShowLine(currentDialogue[0]);
        }

        void Update()
        {
            // Tap to advance
            bool tapped = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                tapped = true;

            if (tapped)
            {
                if (isTyping)
                {
                    skipRequested = true;
                }
                else
                {
                    NextLine();
                }
            }
        }

        void ShowLine(DialogueEntry entry)
        {
            if (speakerNameText != null)
                speakerNameText.text = entry.speakerName;

            if (speakerPortrait != null && entry.speakerPortrait != null)
            {
                speakerPortrait.sprite = entry.speakerPortrait;
                speakerPortrait.enabled = true;
            }
            else if (speakerPortrait != null)
            {
                speakerPortrait.enabled = false;
            }

            // Position portrait based on speaker side
            if (speakerPortrait != null)
            {
                var rt = speakerPortrait.rectTransform;
                if (entry.isPlayerSpeaking && leftPortraitSlot != null)
                    rt.anchoredPosition = leftPortraitSlot.anchoredPosition;
                else if (!entry.isPlayerSpeaking && rightPortraitSlot != null)
                    rt.anchoredPosition = rightPortraitSlot.anchoredPosition;
            }

            if (tapPrompt != null)
                tapPrompt.SetActive(false);

            if (typeCoroutine != null)
                StopCoroutine(typeCoroutine);
            typeCoroutine = StartCoroutine(TypeText(entry.text));
        }

        IEnumerator TypeText(string fullText)
        {
            isTyping = true;
            skipRequested = false;
            dialogueText.text = "";

            foreach (char c in fullText)
            {
                if (skipRequested)
                {
                    dialogueText.text = fullText;
                    break;
                }
                dialogueText.text += c;
                yield return new WaitForSecondsRealtime(typeSpeed);
            }

            isTyping = false;
            if (tapPrompt != null)
                tapPrompt.SetActive(true);
        }

        void NextLine()
        {
            currentLineIndex++;
            if (currentLineIndex < currentDialogue.Length)
            {
                ShowLine(currentDialogue[currentLineIndex]);
            }
            else
            {
                StartCoroutine(FadeOutAndComplete());
            }
        }

        void OnDialogueComplete()
        {
            if (isIntro)
            {
                // After intro dialogue, start the fight
                isIntro = false;
                StoryManager.Instance.StartFight();
            }
            else
            {
                // After outro dialogue, advance to next chapter
                StoryManager.Instance.AdvanceToNextChapter();
            }
        }

        IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0;
            float t = 0;
            while (t < fadeSpeed)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = t / fadeSpeed;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }

        IEnumerator FadeOutAndComplete()
        {
            if (canvasGroup != null)
            {
                float t = 0;
                while (t < fadeSpeed)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = 1 - (t / fadeSpeed);
                    yield return null;
                }
            }
            OnDialogueComplete();
        }
    }
}
