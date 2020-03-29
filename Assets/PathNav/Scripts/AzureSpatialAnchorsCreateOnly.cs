// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

using moveTo = MoveTo;
using System.Net.Http;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class AzureSpatialAnchorsCreateOnly : DemoScriptBase
    {
        internal enum AppState
        {
            DemoStepChooseFlow = 0,
            DemoStepInputAnchorNumber,
            DemoStepCreateSession,
            DemoStepConfigSession,
            DemoStepStartSession,
            DemoStepCreateLocalAnchor,
            DemoStepSaveCloudAnchor,
            DemoStepSavingCloudAnchor,
            DemoStepStopSession,
            DemoStepDestroySession,
            DemoStepCreateSessionForQuery,
            DemoStepStartSessionForQuery,
            DemoStepLookForAnchor,
            DemoStepLookingForAnchor,
            DemoStepStopSessionForQuery,
            DemoStepComplete,
        }

        internal enum DemoFlow
        {
            CreateFlow = 0,
            LocateFlow
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepChooseFlow,new DemoStepParams() { StepMessage = "Next: Choose your Demo Flow", StepColor = Color.clear }},
            { AppState.DemoStepInputAnchorNumber,new DemoStepParams() { StepMessage = "Next: Input anchor number", StepColor = Color.clear }},
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepConfigSession,new DemoStepParams() { StepMessage = "Next: Configure CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepStartSession,new DemoStepParams() { StepMessage = "Next: Start CloudSpatialAnchorSession", StepColor = Color.clear }},
            { AppState.DemoStepCreateLocalAnchor,new DemoStepParams() { StepMessage = "Tap a surface to add the local anchor.", StepColor = Color.blue }},
            { AppState.DemoStepSaveCloudAnchor,new DemoStepParams() { StepMessage = "Next: Save local anchor to cloud", StepColor = Color.yellow }},
            { AppState.DemoStepSavingCloudAnchor,new DemoStepParams() { StepMessage = "Saving local anchor to cloud...", StepColor = Color.yellow }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop cloud anchor session", StepColor = Color.green }},
            { AppState.DemoStepDestroySession,new DemoStepParams() { StepMessage = "Next: Destroy Cloud Anchor session", StepColor = Color.clear }},
            { AppState.DemoStepCreateSessionForQuery,new DemoStepParams() { StepMessage = "Next: Create CloudSpatialAnchorSession for query", StepColor = Color.clear }},
            { AppState.DemoStepStartSessionForQuery,new DemoStepParams() { StepMessage = "Next: Start CloudSpatialAnchorSession for query", StepColor = Color.clear }},
            { AppState.DemoStepLookForAnchor,new DemoStepParams() { StepMessage = "Next: Look for anchor", StepColor = Color.clear }},
            { AppState.DemoStepLookingForAnchor,new DemoStepParams() { StepMessage = "Looking for anchor...", StepColor = Color.clear }},
            { AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Next: Stop CloudSpatialAnchorSession for query", StepColor = Color.yellow }},
            //{ AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Anchor has been added to List for navigation. Count is now " + GameObject.Find("listOfFlagsGameObj").GetComponent<ListOps>().flags.Count, StepColor = Color.yellow }},
            { AppState.DemoStepComplete,new DemoStepParams() { StepMessage = "Next: Restart demo", StepColor = Color.clear }}
        };

        #if !UNITY_EDITOR
            public AnchorExchanger anchorExchanger = new AnchorExchanger();
        #endif
        #region Member Variables
        private AppState _currentAppState = AppState.DemoStepChooseFlow;
        private DemoFlow _currentDemoFlow = DemoFlow.CreateFlow;
        private readonly List<GameObject> otherSpawnedObjects = new List<GameObject>();
        private int anchorsLocated = 0;
        private int anchorsExpected = 0;
        private readonly List<string> localAnchorIds = new List<string>();
        //private string _anchorKeyToFind = null;
        private List<string> _anchorKeyToFind = null;
        //private long? _anchorNumberToFind;
        private List<long?> _anchorNumberToFind = null;
        bool navigationStarted = false;

        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The base URL for the sharing service.")]
        private string baseSharingUrl = "https://sharingservice20200308094713.azurewebsites.net";
        #endregion // Unity Inspector Variables

        private AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                if (_currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                    _currentAppState = value;
                    if (spawnedObjectMat != null)
                    {
                        spawnedObjectMat.color = stateParams[_currentAppState].StepColor;
                    }
                    if (feedbackBox == null)
                        Debug.LogFormat("feedbackbox is null******");
                    else
                    feedbackBox.text = stateParams[_currentAppState].StepMessage;
                        EnableCorrectUIControls();
                }
            }
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                CloudSpatialAnchor nextCsa = args.Anchor;
                currentCloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    anchorsLocated++;
                    currentCloudAnchor = nextCsa;
                    Pose anchorPose = Pose.identity;

                    #if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
                    #endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                    GameObject nextObject = SpawnNewAnchoredObject(anchorPose.position, anchorPose.rotation, currentCloudAnchor);
                    //spawnedObjectMat = nextObject.GetComponent<MeshRenderer>().material;
                    Debug.Log("Setting goal in shareddemo");

                    //GameObject.Find("arrow").GetComponent<moveTo>().setGoal(nextObject.transform, nextObject.name);

#if !UNITY_EDITOR

                    //Instead of setting anchor is up as destination, add the game object to the flag list for later use
                    GameObject.Find("listOfFlagsGameObj").GetComponent<ListOps>().addFlag(nextObject);
                    Debug.Log("********************************************added next Object: " + nextObject.transform.position + ". Main camera's location is " + Camera.main.transform.position + ". other position is " + GameObject.Find("CameraParent").transform.position);

                    // Only start navigation if there are destination flags
                    if ((navigationStarted == false) && (GameObject.Find("listOfFlagsGameObj").GetComponent<ListOps>().flags.Count > 0))
                    {
                        GameObject.Find("CameraParent").GetComponent<CaptureDistance>().beginNavigation();
                        navigationStarted = true;
                    }

                    //      AttachTextMesh(nextObject, _anchorNumberToFind);

#endif
                    otherSpawnedObjects.Add(nextObject);

                    if (anchorsLocated >= anchorsExpected)
                    {
                        currentAppState = AppState.DemoStepStopSessionForQuery;
                    }
                });
            }
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            base.Start();
            /*
            HttpClient c = new HttpClient();
            Task<string> t = c.GetStringAsync(BaseSharingUrl + "/api/anchors/all");
            string s = t.Result;
            Console.WriteLine(s);
            */
            Debug.LogError("Clicked button. made it to 178");

            if (!SanityCheckAccessConfiguration())
            {
                XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
                XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject.SetActive(false);
                return;
            }

            SpatialAnchorSamplesConfig samplesConfig = Resources.Load<SpatialAnchorSamplesConfig>("SpatialAnchorSamplesConfig");
            if (string.IsNullOrWhiteSpace(BaseSharingUrl) && samplesConfig != null)
            {
                BaseSharingUrl = samplesConfig.BaseSharingURL;
            }

            if (string.IsNullOrEmpty(BaseSharingUrl))
            {
                feedbackBox.text = $"Need to set {nameof(BaseSharingUrl)}.";
                XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
                XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject.SetActive(false);
                return;
            }
            else
            {
                Uri result;
                if (!Uri.TryCreate(BaseSharingUrl, UriKind.Absolute, out result))
                {
                    feedbackBox.text = $"{nameof(BaseSharingUrl)} is not a valid url";
                    return;
                }
                else
                {
                    BaseSharingUrl = $"{result.Scheme}://{result.Host}/api/anchors";
                }
            }
            Debug.LogError("Clicked button. made it to 215");

            #if !UNITY_EDITOR
            anchorExchanger.WatchKeys(BaseSharingUrl);
            #endif

            feedbackBox.text = stateParams[currentAppState].StepMessage;

            EnableCorrectUIControls();

            Debug.LogError("Clicked button. made it to 225");

        }

        public async void createAnchorButtonClicked()
        {
            currentAppState = AppState.DemoStepCreateSession;
            await AdvanceCreateFlowDemoAsync();
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = 0f;
                if (CloudManager.SessionStatus != null)
                {
                    createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                }
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
                spawnedObjectMat.color = stateParams[currentAppState].StepColor * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

        protected override Color GetStepColor()
        {
            if (currentCloudAnchor == null || localAnchorIds.Contains(currentCloudAnchor.Identifier))
            {
                return stateParams[currentAppState].StepColor;
            }

            return Color.magenta;
        }

        private void AttachTextMesh(GameObject parentObject, long? dataToAttach)
        {
            GameObject go = new GameObject();

            TextMesh tm = go.AddComponent<TextMesh>();
            if (!dataToAttach.HasValue)
            {
                tm.text = string.Format("{0}:{1}", localAnchorIds.Contains(currentCloudAnchor.Identifier) ? "L" : "R", currentCloudAnchor.Identifier);
            }
            else if (dataToAttach != -1)
            {
                tm.text = $"Anchor Number:{dataToAttach}";
            }
            else
            {
                tm.text = $"Failed to store the anchor key using '{BaseSharingUrl}'";
            }
            tm.fontSize = 32;
            go.transform.SetParent(parentObject.transform, false);
            go.transform.localPosition = Vector3.one * 0.25f;
            go.transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
            go.transform.localScale = Vector3.one * .1f;

            otherSpawnedObjects.Add(go);
        }

#pragma warning disable CS1998 // Conditional compile statements are removing await
        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
#pragma warning restore CS1998

        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            long anchorNumber = -1;

            localAnchorIds.Add(currentCloudAnchor.Identifier);

            #if !UNITY_EDITOR
            anchorNumber = (await anchorExchanger.StoreAnchorKey(currentCloudAnchor.Identifier));
            #endif

            Pose anchorPose = Pose.identity;

            #if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
            #endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            AttachTextMesh(spawnedObject, anchorNumber);

            currentAppState = AppState.DemoStepStopSession;

            feedbackBox.text = $"Created anchor {anchorNumber}. Next: Stop cloud anchor session";
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);
        }

        public async override Task AdvanceDemoAsync()
        {
            if (currentAppState == AppState.DemoStepChooseFlow || currentAppState == AppState.DemoStepInputAnchorNumber)
            {
                return;
            }

            if (_currentDemoFlow == DemoFlow.CreateFlow)
            {
                await AdvanceCreateFlowDemoAsync();
            }
            else if (_currentDemoFlow == DemoFlow.LocateFlow)
            {
                await AdvanceLocateFlowDemoAsync();
            }
        }

        public async Task InitializeCreateFlowDemoAsync()
        {
            if (currentAppState == AppState.DemoStepChooseFlow)
            {
                _currentDemoFlow = DemoFlow.CreateFlow;
                currentAppState = AppState.DemoStepCreateSession;
            }
            else
            {
                await AdvanceDemoAsync();
            }
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeCreateFlowDemo()
        {
            try
            {
                await InitializeCreateFlowDemoAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(AzureSpatialAnchorsCreateOnly)} - Error in {nameof(InitializeCreateFlowDemo)}: {ex.Message}");
            }
        }


#pragma warning disable CS1998 // Conditional compile statements are removing await
        public async Task InitializeLocateFlowDemoAsync()
#pragma warning restore CS1998
        {
            if (currentAppState == AppState.DemoStepChooseFlow)
            {
                currentAppState = AppState.DemoStepInputAnchorNumber;
            }
            else if (currentAppState == AppState.DemoStepInputAnchorNumber)
            {
                long anchorNumber;
                string inputText = XRUXPickerForMainMenu.Instance.GetDemoInputField().text;
                if (!long.TryParse(inputText, out anchorNumber))
                {
                    if (feedbackBox == null)
                        Debug.Log("feedbackbox is null!!!!!!!!");
                    feedbackBox.text = "Invalid Anchor Number!";
                }
                else
                {
                    // _anchorNumberToFind = anchorNumber;
                    // This is where I need to change _anchorKeyToFind to a list and cycle through all rowKeys (of interest. Statically set for now) and add them to _anchorKeyToFindList
                    // For now it will ignore user's actual input
                    _anchorNumberToFind = new List<long?>();
                    _anchorKeyToFind = new List<String>();

                    // Add rowkeys
                    _anchorNumberToFind.Add(17); // Add first flag
                    _anchorNumberToFind.Add(18); // Add second flag
                    _anchorNumberToFind.Add(19); // Add third flag
                    for (int i = 0; i < 3; i++)
                    {
                        // Add anchor keys
#if !UNITY_EDITOR

                        string currentAnchorKey = await anchorExchanger.RetrieveAnchorKey((long)_anchorNumberToFind[i]);
                        _anchorKeyToFind.Add(currentAnchorKey);
#endif

                    }

                    //_anchorKeyToFind = await anchorExchanger.RetrieveAnchorKey(_anchorNumberToFind.Value);
                    if (_anchorKeyToFind == null)
                    {
                        feedbackBox.text = "Anchor Number Not Found!";
                    }
                    else
                    {
                        _currentDemoFlow = DemoFlow.LocateFlow;
                        currentAppState = AppState.DemoStepCreateSession;
                        XRUXPickerForMainMenu.Instance.GetDemoInputField().text = "";
                    }
                }
            }
            else
            {
                await AdvanceDemoAsync();
            }
        }

        /// <summary>
        /// This version only exists for Unity to wire up a button click to.
        /// If calling from code, please use the Async version above.
        /// </summary>
        public async void InitializeLocateFlowDemo()
        {
            try
            {
                await InitializeLocateFlowDemoAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(AzureSpatialAnchorsCreateOnly)} - Error in {nameof(InitializeLocateFlowDemo)}: {ex.Message}");
            }
        }

        private async Task AdvanceCreateFlowDemoAsync()
        {
            Debug.LogError("made it inside AdvanceCreateFlowDemoAsync()");

            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    Debug.LogError("assigning currentCloudAnchor to null");
                    Debug.LogError("setting currentAppState to AppState.DemoStepConfigSession");

                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    Debug.LogError("about to configure session");

                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepCreateLocalAnchor;
                    break;
                case AppState.DemoStepCreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.DemoStepSaveCloudAnchor;
                    }
                    break;
                case AppState.DemoStepSaveCloudAnchor:
                    currentAppState = AppState.DemoStepSavingCloudAnchor;
                    await SaveCurrentObjectAnchorToCloudAsync();
                    break;
                case AppState.DemoStepStopSession:
                    CloudManager.StopSession();
                    CleanupSpawnedObjects();
                    await CloudManager.ResetSessionAsync();
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepChooseFlow;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState);
                    break;
            }
        }

        private async Task AdvanceLocateFlowDemoAsync()
        {
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    currentAppState = AppState.DemoStepChooseFlow;
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    anchorsLocated = 0;
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    currentAppState = AppState.DemoStepLookingForAnchor;
                    currentWatcher = CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchor:
                    // Advancement will take place when anchors have all been located.
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    CloudManager.StopSession();
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentWatcher = null;
                    currentAppState = AppState.DemoStepChooseFlow;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState);
                    break;
            }
        }

        private void EnableCorrectUIControls()
        {

            Debug.Log("Buttons labels are the following" + XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].name + ", " + XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].name +
                XRUXPickerForMainMenu.Instance.GetDemoButtons()[2].name);
            if (currentAppState == null)
                Debug.Log("in EnableCorrectUIControls(), currentAppState is null");
            else
                Debug.Log("in EnableCorrectUIControls(), currentAppState is " + currentAppState);
            // if (XRUXPickerForMainMenu == null)
            //    Debug.Log("********************************XRUXPickerForMainMenu is null!!****************************");
            if (XRUXPickerForMainMenu.Instance == null)
              Debug.Log("********************************XRUXPickerForMainMenu.Instance is " + XRUXPickerForMainMenu.Instance);


            if (XRUXPickerForMainMenu.Instance.GetDemoButtons()[0] == null)
            {
                Debug.Log("********************************first button is null!!****************************");
            }
            if (XRUXPickerForMainMenu.Instance.GetDemoButtons()[1] == null)
            {
                Debug.Log("********************************2nd button is null!!****************************");
            }

            switch (currentAppState)
            {
                case AppState.DemoStepChooseFlow:

                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject.SetActive(true);
#if UNITY_WSA
                    XRUXPickerForMainMenu.Instance.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.1f;
                    XRUXPickerForMainMenu.Instance.transform.LookAt(Camera.main.transform);
                    XRUXPickerForMainMenu.Instance.transform.Rotate(Vector3.up, 180);
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(true);
#else
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].transform.Find("Text").GetComponent<Text>().text = "Create & Share Anchor";
#endif
                    XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject.SetActive(false);
                    break;
                case AppState.DemoStepInputAnchorNumber:
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject.SetActive(true);
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
                    XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject.SetActive(true);
                    break;
                default:
                    if (XRUXPickerForMainMenu.Instance.GetDemoButtons() == null)
                        Debug.Log("XRUXPickerForMainMenu.Instance.GetDemoButtons() is null");
                    else if (XRUXPickerForMainMenu.Instance.GetDemoButtons()[1] == null)
                        Debug.Log("XRUXPickerForMainMenu.Instance.GetDemoButtons()[1] is null");
                    else if (XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject == null)
                        Debug.Log("XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject is null");
                    else
                        XRUXPickerForMainMenu.Instance.GetDemoButtons()[1].gameObject.SetActive(false);
#if UNITY_WSA
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(false);
#else
                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].gameObject.SetActive(true);

                    XRUXPickerForMainMenu.Instance.GetDemoButtons()[0].transform.Find("Text").GetComponent<Text>().text = "Next Step";
                    #endif
                    if ((XRUXPickerForMainMenu.Instance.GetDemoInputField() == null) || (XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject == null))
                        Debug.Log("XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject is null");
                    else
                        XRUXPickerForMainMenu.Instance.GetDemoInputField().gameObject.SetActive(false); // "enter cloud anchor session for query.."
                    break;
            }
            
        }

        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();
            Debug.Log("Configuring session.. with currentAppState being set to " + AppState.DemoStepCreateSessionForQuery);

            if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            {
                Debug.Log("*** at lin 574, _anchorKeyTOFind is " + (_anchorKeyToFind == null ? "null" : "not null"));

                // Should change logic to go through all of the _anchorsToFindList, and set 
                // Remember that _anchorKeyToFind is not the same as rowkey!
                for (int i = 0; i < _anchorKeyToFind.Count; i++)
                    anchorsToFind.Add(_anchorKeyToFind[i]);
            }
            {
                anchorsExpected = anchorsToFind.Count;


                SetAnchorIdsToLocate(anchorsToFind);

            }
        }

        protected override void CleanupSpawnedObjects()
        {
            base.CleanupSpawnedObjects();

            for (int index = 0; index < otherSpawnedObjects.Count; index++)
            {
                Destroy(otherSpawnedObjects[index]);
            }

            otherSpawnedObjects.Clear();
        }

        /// <summary>
        /// Gets or sets the base URL for the example sharing service.
        /// </summary>
        public string BaseSharingUrl { get => baseSharingUrl; set => baseSharingUrl = value; }
    }
}