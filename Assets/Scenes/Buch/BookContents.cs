using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Convai.Scripts.Runtime.Features;

namespace ChristinaCreatesGames.Typography.Book
{
    public class BookContents : MonoBehaviour
    {
        [TextArea(10,20)]
        [SerializeField] private string content;
        [Space]
        [SerializeField] private TMP_Text leftSide;
        [SerializeField] private TMP_Text rightSide;
        [Space]
        [SerializeField] private TMP_Text leftPagination;
        [SerializeField] private TMP_Text rightPagination;

        [Header("UI Controls")]
        [Tooltip("Das GameObject, das den Next-Page-Knopf enthält")]
        [SerializeField] private GameObject nextPageObject;

        [Header("Narrative Design Trigger")]
        [SerializeField] private NarrativeDesignTrigger narrativeTrigger;

        [Tooltip("Liste der Seitenzahlen und zugehörigen ConvAI‑Triggernamen")]
        [SerializeField] private List<PageTrigger> pageTriggers = new();
        private Dictionary<int, string> _triggerDictionary;

        [System.Serializable]
        public class PageTrigger
        {
            public int pageNumber;
            public string eventName;
        }

        [Header("Choice → neuer Text")]
        [Tooltip("Mapping von Section-ID zu neuem Buchtext")]
        [SerializeField] private List<ChoiceContent> choiceContents = new();
        private Dictionary<string, string> _choiceMap;

        [System.Serializable]
        public class ChoiceContent
        {
            public string choiceName;    // hier die Section-ID aus ConvAI
            [TextArea(5,10)]
            public string newContent;    // der Text, der danach im Buch erscheinen soll
        }

        private void Awake()
        {
            // Initialer Aufbau
            SetupContent();
            UpdatePagination();
            BuildTriggerDictionary();
            BuildChoiceMap();
        }

        private void Start()
        {
            StartCoroutine(InitSectionSubscriptions());
        }

        private IEnumerator InitSectionSubscriptions()
        {
            yield return new WaitUntil(() =>
                narrativeTrigger != null
                && narrativeTrigger.convaiNPC != null
                && narrativeTrigger.convaiNPC.narrativeDesignManager != null
                && narrativeTrigger.convaiNPC.narrativeDesignManager.sectionChangeEventsDataList != null
                && narrativeTrigger.convaiNPC.narrativeDesignManager.sectionChangeEventsDataList.Count > 0
            );

            SubscribeSectionEvents();
        }

        private void BuildTriggerDictionary()
        {
            _triggerDictionary = new Dictionary<int, string>();
            foreach (var pt in pageTriggers)
                if (!_triggerDictionary.ContainsKey(pt.pageNumber))
                    _triggerDictionary.Add(pt.pageNumber, pt.eventName);
        }

        private void BuildChoiceMap()
        {
            _choiceMap = new Dictionary<string, string>();
            foreach (var cc in choiceContents)
                if (!_choiceMap.ContainsKey(cc.choiceName))
                    _choiceMap.Add(cc.choiceName, cc.newContent);
        }

        private void SetupContent()
        {
            leftSide.text  = content;
            rightSide.text = content;
        }

        private void UpdatePagination()
        {
            leftPagination .text = leftSide .pageToDisplay.ToString();
            rightPagination.text = rightSide.pageToDisplay.ToString();
        }

        public void PreviousPage()
        {
            if (leftSide.pageToDisplay < 1)
                leftSide.pageToDisplay = 1;
            else if (leftSide.pageToDisplay - 2 > 1)
                leftSide.pageToDisplay -= 2;
            else
                leftSide.pageToDisplay = 1;

            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            UpdatePagination();
            CheckForTrigger();
        }

        public void NextPage()
        {
            if (rightSide.pageToDisplay >= rightSide.textInfo.pageCount)
                return;

            if (leftSide.pageToDisplay >= leftSide.textInfo.pageCount - 1)
                leftSide.pageToDisplay = leftSide.textInfo.pageCount - 1;
            else
                leftSide.pageToDisplay += 2;

            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            UpdatePagination();
            CheckForTrigger();
        }

        private void CheckForTrigger()
        {
            int currentPage = leftSide.pageToDisplay;
            if (_triggerDictionary != null
                && _triggerDictionary.TryGetValue(currentPage, out string eventName))
            {
                narrativeTrigger.UpdateAvailableTriggers();

                int idx = narrativeTrigger.availableTriggers.IndexOf(eventName);
                if (idx >= 0)
                {
                    narrativeTrigger.selectedTriggerIndex = idx;
                    narrativeTrigger.InvokeSelectedTrigger();
                    Debug.Log($"[BookContents] ConvAI‑Trigger '{eventName}' (Index {idx}) auf Seite {currentPage} gesendet.");

                    // NextPage-Knopf ausblenden
                    if (nextPageObject != null)
                        nextPageObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"[BookContents] ConvAI‑Trigger '{eventName}' nicht in availableTriggers gefunden.");
                }
            }
        }

        private void SubscribeSectionEvents()
        {
            var ndManager = narrativeTrigger.convaiNPC.narrativeDesignManager;
            Debug.Log($"[BookContents] Registriere {ndManager.sectionChangeEventsDataList.Count} Section-Events.");

            foreach (var sc in ndManager.sectionChangeEventsDataList)
            {
                string sectionId = sc.id;
                sc.onSectionStart.AddListener(() => HandleSectionStart(sectionId));
                Debug.Log($"[BookContents] Listener für Section '{sectionId}' registriert.");
            }
        }

        private void HandleSectionStart(string sectionId)
        {
            Debug.Log($"[BookContents] Section-Start empfangen: '{sectionId}'");

            if (_choiceMap.TryGetValue(sectionId, out string nextText))
            {
                content = nextText;
                SetupContent();
                UpdatePagination();
                Debug.Log($"[BookContents] Text für Section '{sectionId}' aktualisiert.");

                // NextPage-Knopf wieder einblenden
                if (nextPageObject != null)
                    nextPageObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[BookContents] Kein Text-Mapping für Section '{sectionId}' gefunden.");
            }
        }
    }
}