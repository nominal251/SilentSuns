using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Builds only the computer's screen. MasterComputerStation handles the ship and interaction.
    public class MasterComputerUI : MonoBehaviour
    {
        public enum ScreenPage { Status, Logs, Tools, Alerts }
        public MasterComputerData Data { get; private set; }
        public ScreenPage Page { get; private set; }
        public bool Ready { get; private set; }
        public bool Initialized { get; private set; }
        public bool InUse { get; private set; }
        public bool AlertBlocking => Data != null && Data.UnreadCount > 0 && Page != ScreenPage.Alerts;
        public float Power { get; private set; }
        public float Warmup { get; private set; } = 1;
        public bool Intrusion { get; private set; }
        public const float IntrusionDuration = 2.2f;
        public float FaultStrength { get; private set; }
        public float TransitionProgress { get; private set; } = -1;
        public bool Transitioning => TransitionProgress >= 0;
        public event System.Action<bool> IntrusionChanged;
        public event System.Action ExitRequested;

        private readonly Color muted = new Color32(147, 171, 210, 255);
        private readonly Color warning = new Color32(255, 202, 88, 255);
        private readonly Color good = new Color32(131, 218, 166, 255);
        private readonly Color cyan = new Color32(132, 207, 236, 255);
        private readonly Color panel = new Color32(10, 18, 38, 255);
        private TMP_Text completion, currentSystem, recordCount;
        private TMP_Text hullCaption, catalogState, recordStamp, recordSection;
        private readonly TMP_Text[] resourceValues = new TMP_Text[3];
        private Image recordAccent;
        private MasterComputerGraphic recordSymbol;
        private Image surveyFill, hullFill;
        private MasterComputerGraphic shipDrawing;
        private Transform pageRoot, warningRoot, bootRoot;
        private TMP_Text pageTitle, shipStatus, survey, bootText, alertText, details, detailTitle, detailMeta, pageNumber;
        private TMP_Text bootStatus;
        private readonly Image[] bootSteps = new Image[4];
        private string bootHistory;
        private CanvasGroup controls, pageFade, screenContents;
        private TerminalButton submit, reviewAlert, exit, previous, next;
        private RawImage intrusionImage;
        private Texture face;
        private float intrusionChance;
        private Coroutine startup, pageChange;
        private bool armedIntrusion;
        private Image[] recordMarks = new Image[6];
        private ScrollRect detailScroll;
        private RectTransform detailContent;
        private int listPage, selected = -1, listCount = -1, unread = -1;
        private int discovered, catalogued, total;
        private bool dirty = true;

        public void Bind(MasterComputerData data)
        {
            if (Data != null) Data.Changed -= OnDataChanged;
            Data = data;
            if (Data != null) Data.Changed += OnDataChanged;
            dirty = true;
        }

        // The station reads the existing PlanetManager and sends the counts here.
        public void SetSurvey(int found, int complete, int count)
        {
            discovered = found; catalogued = complete; total = count;
        }

        private void Start()
        {
            Build();
            Initialized = true;
        }

        public void ConfigureStartup(Texture image, float chance) { face = image; intrusionChance = chance; }
#if UNITY_EDITOR
        // Tests can trigger the rare event directly. Player builds have no testing shortcut.
        public void ArmStartupIntrusion() => armedIntrusion = true;
#endif

        private TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, float size = 26)
        {
            var label = TerminalUI.Text(name, parent, text, x, y, w, h, size);
            label.richText = false;
            return label;
        }

        private TerminalButton Button(Transform parent, string text, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        {
            var button = TerminalUI.Button(parent, text, x, y, w, h, action);
            // Unavailable controls stay dark, so they cannot look like the selected page.
            var colors = button.colors;
            colors.disabledColor = new Color32(24, 29, 71, 255);
            button.colors = colors;
            return button;
        }

        private Transform Frame(string name, Transform parent, float x, float y, float w, float h)
        {
            var edge = TerminalUI.Panel(name, parent, x, y, w, h, TerminalUI.Ink);
            var inside = TerminalUI.Panel("Inside", edge.transform, 0, 0, 1, 1, TerminalUI.Dark).rectTransform;
            // Keep the same line weight on tall panels and short resource strips.
            inside.offsetMin = new Vector2(1.5f, 1.5f);
            inside.offsetMax = new Vector2(-1.5f, -1.5f);
            return inside;
        }

        private Color PageColor => Page == ScreenPage.Tools ? cyan : Page == ScreenPage.Alerts ? warning : Page == ScreenPage.Logs ? new Color32(225, 207, 160, 255) : good;

        private void Section(Transform parent, string text, Color tint)
        {
            TerminalUI.Panel("Section band", parent, 0, 0, 1, .13f, panel);
            TerminalUI.Panel("Section accent", parent, 0, 0, .008f, .13f, tint);
            Label(parent, "Section", text, .035f, .025f, .93f, .08f, 26).color = tint;
        }

        private MasterComputerGraphic Drawing(Transform parent, string name, MasterComputerGraphic.Drawing kind, float x, float y, float w, float h, Color tint)
        {
            var graphic = TerminalUI.Rect(name, parent, x, y, w, h).gameObject.AddComponent<MasterComputerGraphic>();
            graphic.drawing = kind; graphic.color = tint; graphic.raycastTarget = false;
            return graphic;
        }

        private void Build()
        {
            TerminalUI.Panel("Idle glass", transform, 0, 0, 1, 1, Color.black);
            var screen = TerminalUI.Rect("Screen contents", transform, 0, 0, 1, 1);
            screenContents = screen.gameObject.AddComponent<CanvasGroup>();
            TerminalUI.Panel("Background", screen, 0, 0, 1, 1, TerminalUI.Dark);
            TerminalUI.Panel("Title band", screen, .02f, .02f, .96f, .08f, TerminalUI.Blue);
            Label(screen, "Computer title", "ARCA / MASTER CONTROL", .035f, .025f, .67f, .065f, 32);
            pageTitle = Label(screen, "Page title", "00 / STATUS", .71f, .025f, .255f, .065f, 28);
            pageTitle.alignment = TextAlignmentOptions.Right;
            TerminalUI.Panel("Header rule", screen, .035f, .108f, .93f, .003f, muted);
            var main = TerminalUI.Rect("Controls", screen, 0, 0, 1, 1);
            controls = main.gameObject.AddComponent<CanvasGroup>();
            pageRoot = TerminalUI.Rect("Page", main, .035f, .15f, .93f, .73f);
            pageFade = pageRoot.gameObject.AddComponent<CanvasGroup>();
            exit = Button(main, "EXIT COMPUTER", .76f, .906f, .205f, .065f, () => ExitRequested?.Invoke());

            warningRoot = Frame("Incoming alert", screen, .10f, .23f, .80f, .64f);
            warningRoot.gameObject.AddComponent<RectMask2D>();
            Drawing(warningRoot, "Top caution tape", MasterComputerGraphic.Drawing.Tape, 0, .025f, 1, .055f, warning);
            Drawing(warningRoot, "Caution symbol", MasterComputerGraphic.Drawing.Caution, .07f, .20f, .24f, .45f, warning);
            Label(warningRoot, "Caution", "CAUTION", .37f, .19f, .59f, .15f, 50).color = warning;
            alertText = Label(warningRoot, "Alert summary", "NEW SHIP ALERT", .37f, .37f, .59f, .14f, 30);
            Label(warningRoot, "Review instruction", "CHECK ALERTS TO CONTINUE.", .37f, .53f, .59f, .12f, 25);
            reviewAlert = Button(warningRoot, "REVIEW ALERTS  >", .07f, .75f, .86f, .13f, () => SelectPage(3));
            Drawing(warningRoot, "Bottom caution tape", MasterComputerGraphic.Drawing.Tape, 0, .93f, 1, .055f, warning);
            warningRoot.parent.gameObject.SetActive(false);

            bootRoot = TerminalUI.Panel("Startup", transform, 0, 0, 1, 1, TerminalUI.Dark).transform;
            TerminalUI.Panel("Startup title band", bootRoot, .035f, .035f, .93f, .105f, TerminalUI.Blue);
            Label(bootRoot, "Login heading", "ARCA / MASTER COMPUTER", .055f, .045f, .68f, .08f, 36);
            var identity = Label(bootRoot, "Terminal identity", "MC-01", .76f, .045f, .185f, .08f, 27);
            identity.alignment = TextAlignmentOptions.Right;
            Label(bootRoot, "Computer purpose", "SHIP RECORDS / SURVEY CONTROL", .055f, .165f, .89f, .045f, 26).color = cyan;
            var console = Frame("Access console", bootRoot, .035f, .25f, .93f, .61f);
            bootText = Label(console, "Credentials", "", .025f, .03f, .95f, .94f, 27);
            bootText.alignment = TextAlignmentOptions.TopLeft;
            bootStatus = Label(bootRoot, "Startup status", "", .055f, .90f, .67f, .055f, 25);
            bootStatus.color = muted;
            for (int i = 0; i < bootSteps.Length; i++)
                bootSteps[i] = TerminalUI.Panel("Startup step " + i, bootRoot, .785f + i * .043f, .918f, .033f, .018f, TerminalUI.Blue);
            intrusionImage = TerminalUI.Rect("Signal intrusion", transform, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            intrusionImage.texture = face;
            intrusionImage.color = new Color(.65f, .65f, .65f, 1);
            intrusionImage.raycastTarget = false;
            intrusionImage.gameObject.SetActive(false);
            BuildPage();
            PowerOff();
        }

        private IEnumerator Boot(bool showIntrusion)
        {
            Ready = false;
            screenContents.alpha = 0;
            bootRoot.gameObject.SetActive(true);
            bootHistory = "ARCA/OS 4.2  /  LOCAL MASTER TERMINAL\n\n";
            bootText.text = bootHistory + "_";
            SetBootStep(0, "ESTABLISHING LOCAL SESSION");
            // The CRT warms first. The typed commands then open the computer's actual functions.
            for (float t = 0; t < 1.25f; t += Time.unscaledDeltaTime)
            {
                Power = Mathf.SmoothStep(0, 1, t / 1.25f);
                Warmup = 1 - Mathf.SmoothStep(0, 1, t / 1.25f);
                yield return null;
            }
            Power = 1; Warmup = 0;
            yield return TypeCommand("LOGIN CREW");
            yield return BootResponse("ACCESS GRANTED / SHIPBOARD SESSION", .22f);
            SetBootStep(1, "MOUNTING DOCUMENT ARCHIVE");
            yield return TypeCommand("MOUNT RECORDS");
            yield return BootResponse("LOG ARCHIVE ............. ONLINE\nRESEARCH RECORDS ........ ONLINE", .28f);
            SetBootStep(2, "CONNECTING SHIP SYSTEMS");
            yield return TypeCommand("CONNECT SHIP");
            if (showIntrusion)
            {
                // A held prompt and one short signal cut make the face arrive unexpectedly.
                yield return new WaitForSecondsRealtime(.35f);
                Power = 0;
                yield return new WaitForSecondsRealtime(.16f);
                bootRoot.gameObject.SetActive(false);
                Intrusion = true;
                intrusionImage.gameObject.SetActive(true);
                SetIntrusionFrame(0);
                IntrusionChanged?.Invoke(true);
                for (float t = 0; t < IntrusionDuration; t += Time.unscaledDeltaTime)
                {
                    SetIntrusionFrame(t);
                    yield return null;
                }
                EndIntrusion();
                Power = 0;
                yield return new WaitForSecondsRealtime(.22f);
                bootRoot.gameObject.SetActive(true);
                bootStatus.text = "RESTORING DISPLAY LINK";
                for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
                {
                    Power = Mathf.SmoothStep(0, 1, t / .35f);
                    yield return null;
                }
                Power = 1;
            }
            yield return BootResponse("TELEMETRY / CATALOG / ALERTS ... ONLINE", .22f);
            SetBootStep(3, "OPENING MASTER CONTROL");
            yield return TypeCommand("OPEN MASTER CONTROL");
            SetBootStep(4, "SESSION READY");
            yield return BootResponse("SESSION READY", .4f);
            bootStatus.text = "LOADING " + PageName;
            yield return SettleScreen(true);
            Ready = true;
            startup = null;
            dirty = true;
            RefreshGate();
        }

        private IEnumerator TypeCommand(string command)
        {
            bootHistory += "MC:\\> ";
            for (int i = 0; i < command.Length; i++)
            {
                bootHistory += command[i];
                bootText.text = bootHistory + "_";
                // Use the main-menu key sound and its UI mixer, without stacking a sound per frame.
                if (i % 2 == 0) TerminalInteractionAudio.Instance?.Keyboard();
                yield return new WaitForSecondsRealtime(command[i] == ' ' ? .09f : .038f);
            }
            yield return new WaitForSecondsRealtime(.10f);
            bootHistory += "\n";
        }

        private IEnumerator BootResponse(string text, float hold)
        {
            bootHistory += text + "\n";
            bootText.text = bootHistory + "_";
            yield return new WaitForSecondsRealtime(hold);
        }

        private void SetBootStep(int completed, string status)
        {
            bootStatus.text = status;
            for (int i = 0; i < bootSteps.Length; i++) bootSteps[i].color = i < completed ? cyan : TerminalUI.Blue;
        }

        private void SetIntrusionFrame(float time)
        {
            if (GamePreferences.ReducedMotion)
            {
                intrusionImage.uvRect = new Rect(0, 0, 1, 1);
                intrusionImage.color = new Color(.85f, .85f, .85f, 1);
                FaultStrength = 0; Power = .85f;
                return;
            }
            // Start readable, then push closer and tear the signal. No white-screen flashes.
            float size = time < .14f ? Mathf.Lerp(.80f, .62f, time / .14f) : Mathf.Lerp(.62f, .68f, time / IntrusionDuration);
            float shake = time > .14f && time < .45f ? Mathf.Sin(time * 71) * .008f : 0;
            intrusionImage.uvRect = new Rect((1 - size) * .5f + shake, (1 - size) * .5f, size, size);
            intrusionImage.color = new Color(.95f, .95f, .95f, 1);
            FaultStrength = time < .14f ? .08f : Mathf.Lerp(1, .42f, time / IntrusionDuration);
            Power = time > .54f && time < .63f ? .08f : .88f;
        }

        public void SetInUse(bool value)
        {
            if (InUse == value) return;
            InUse = value;
            if (!Initialized) return;
            if (!value) { PowerOff(); return; }
            bool scare = face != null && (armedIntrusion || (Data != null && Data.TryStartupIntrusion(Random.value, intrusionChance)));
            armedIntrusion = false;
            startup = StartCoroutine(Boot(scare));
            exit.gameObject.SetActive(true);
            RefreshGate();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void PowerOff()
        {
            if (startup != null) { StopCoroutine(startup); startup = null; }
            if (pageChange != null) { StopCoroutine(pageChange); pageChange = null; }
            TransitionProgress = -1;
            EndIntrusion();
            Ready = false; Power = 0; Warmup = 1;
            // The physical dial follows Page, so every way out returns it to 0 / Status.
            Page = ScreenPage.Status;
            listPage = 0; selected = -1;
            screenContents.alpha = 0;
            bootRoot.gameObject.SetActive(false);
            exit.gameObject.SetActive(false);
            RefreshGate();
        }

        private void EndIntrusion()
        {
            bool wasActive = Intrusion;
            Intrusion = false; FaultStrength = 0;
            if (intrusionImage != null) intrusionImage.gameObject.SetActive(false);
            if (wasActive) IntrusionChanged?.Invoke(false);
        }

        // Also accepts a 0-3 index from the console's rotary selector.
        public void SelectPage(int page)
        {
            if (!Ready || page < 0 || page > 3 || (AlertBlocking && page != 3)) return;
            if (page == (int)Page) return;
            if (pageChange != null) StopCoroutine(pageChange);
            Page = (ScreenPage)page;
            listPage = 0; selected = -1;
            pageChange = StartCoroutine(SettleScreen(false));
        }

        private string PageName => Page == ScreenPage.Tools ? "R&D" : Page.ToString().ToUpperInvariant();

        private IEnumerator SettleScreen(bool fromBoot)
        {
            // Hold the old picture briefly, then load the latest dial position. No blank frame.
            TransitionProgress = 0;
            float hold = GamePreferences.ReducedMotion ? .12f : fromBoot ? .30f : Random.Range(.18f, .28f);
            float settle = GamePreferences.ReducedMotion ? .10f : fromBoot ? .40f : Random.Range(.24f, .34f);
            if (!fromBoot) pageTitle.text = $"{(int)Page:00} / ACCESSING";
            RefreshGate();
            for (float t = 0; t < hold; t += Time.unscaledDeltaTime)
            {
                TransitionProgress = .4f * t / hold;
                yield return null;
            }
            // BuildPage fills the real readings before the picture is handed over.
            BuildPage();
            bootRoot.gameObject.SetActive(false);
            screenContents.alpha = 1;
            for (float t = 0; t < settle; t += Time.unscaledDeltaTime)
            {
                TransitionProgress = Mathf.Lerp(.4f, 1, t / settle);
                yield return null;
            }
            TransitionProgress = -1;
            pageChange = null;
            RefreshGate();
        }

        private void BuildPage()
        {
            foreach (Transform child in pageRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            pageTitle.text = $"{(int)Page:00} / " + PageName;
            pageTitle.color = PageColor;
            if (Page == ScreenPage.Status) { BuildStatus(); RefreshStatus(); } else BuildList();
            dirty = true;
        }

        private void BuildStatus()
        {
            var left = Frame("Ship status", pageRoot, 0, 0, .59f, .70f);
            Section(left, "SHIP INTEGRITY", good);
            Drawing(left, "Diagram grid", MasterComputerGraphic.Drawing.Grid, .03f, .16f, .94f, .59f, new Color32(30, 56, 65, 255));
            shipDrawing = Drawing(left, "Ship outline", MasterComputerGraphic.Drawing.Ship, .08f, .17f, .84f, .53f, good);
            Label(left, "Diagram caption", "ARCA / OVERALL HULL", .045f, .72f, .40f, .075f, 22).color = muted;
            shipStatus = Label(left, "Hull", "AWAITING TELEMETRY", .44f, .70f, .515f, .12f, 33);
            shipStatus.alignment = TextAlignmentOptions.Right;
            var hullTrack = TerminalUI.Panel("Hull track", left, .045f, .85f, .91f, .035f, panel);
            hullFill = TerminalUI.Panel("Hull fill", hullTrack.transform, 0, 0, 1, 1, good);
            // These are divisions of total hull health, not separate damage readings for each module.
            for (int i = 1; i < 24; i++) TerminalUI.Panel("Hull division", hullTrack.transform, i / 24f, 0, .004f, 1, TerminalUI.Dark);
            hullCaption = Label(left, "Hull condition", "", .045f, .91f, .91f, .065f, 20);
            hullCaption.color = muted;

            var middle = Frame("Survey status", pageRoot, .61f, 0, .39f, .70f);
            Section(middle, "SYSTEM SURVEY", cyan);
            currentSystem = Label(middle, "Current system", "", .055f, .18f, .89f, .08f, 25);
            completion = Label(middle, "Completion", "", .055f, .28f, .89f, .16f, 42);
            completion.color = cyan;
            var surveyTrack = TerminalUI.Panel("Survey track", middle, .055f, .47f, .89f, .025f, panel);
            surveyFill = TerminalUI.Panel("Survey fill", surveyTrack.transform, 0, 0, 0, 1, cyan);
            survey = Label(middle, "Survey counts", "", .055f, .54f, .89f, .17f, 25);
            survey.alignment = TextAlignmentOptions.TopLeft;
            catalogState = Label(middle, "Catalog state", "", .055f, .72f, .89f, .07f, 21);
            catalogState.color = muted;
            submit = Button(middle, "SUBMIT CATALOG  >", .055f, .82f, .89f, .13f, SubmitCatalog);

            var resources = Frame("Resources", pageRoot, 0, .74f, 1, .26f);
            string[] names = { "CRYO CHARGES", "SHIP PARTS", "PARANORMAL ENERGY" };
            Color[] colors = { cyan, good, warning };
            float width = 1f / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                float x = i * width;
                if (i > 0) TerminalUI.Panel("Resource divider", resources, x, .16f, .001f, .68f, muted * .4f);
                Label(resources, "Resource label " + i, names[i], x + .025f, .13f, width - .05f, .23f, 23).color = muted;
                resourceValues[i] = Label(resources, "Resource value " + i, "--", x + .025f, .39f, width - .05f, .46f, 43);
                resourceValues[i].color = colors[i];
            }
        }

        private void SubmitCatalog()
        {
            if (Ready && InUse && !Transitioning && !AlertBlocking && Data != null) Data.Game?.SubmitCatalog();
        }

        private int Count => Data == null ? 0 : Page == ScreenPage.Logs ? Data.Logs.Count : Page == ScreenPage.Tools ? Data.Tools.Count : Data.Alerts.Count;

        private void BuildList()
        {
            var list = Frame("Record list", pageRoot, 0, 0, .30f, 1);
            int count = Count;
            listCount = count;
            listPage = Mathf.Clamp(listPage, 0, Mathf.Max(0, (count - 1) / 6));
            Section(list, Page == ScreenPage.Logs ? "LOG ARCHIVE" : Page == ScreenPage.Tools ? "R&D / EQUIPMENT" : "SHIP MESSAGES", PageColor);
            recordCount = Label(list, "Count", Page == ScreenPage.Alerts ? $"{count:00} FILED / {Data?.UnreadCount ?? 0:00} UNREAD" : $"{count:00} RECORDS STORED", .055f, .14f, .89f, .06f, 21);
            recordCount.color = muted;
            System.Array.Clear(recordMarks, 0, recordMarks.Length);
            for (int i = listPage * 6; i < Mathf.Min(count, listPage * 6 + 6); i++)
            {
                int index = i;
                string title = Page == ScreenPage.Alerts ? (Data.Alerts[i].read ? "  " : "! ") + Data.Alerts[i].title : Record(i).title;
                float y = .23f + (i % 6) * .103f;
                var button = Button(list, $"{i + 1:00}  " + Upper(title), .045f, y, .91f, .093f, () => SelectRecord(index));
                var colors = button.colors;
                colors.normalColor = panel;
                button.colors = colors;
                recordMarks[i % 6] = TerminalUI.Panel("Selected record", button.transform, 0, 0, .014f, 1, PageColor);
                recordMarks[i % 6].raycastTarget = false;
                recordMarks[i % 6].enabled = i == selected;
                var label = button.transform.Find("Label").GetComponent<TMP_Text>();
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.maxVisibleLines = 2;
            }
            if (count == 0)
            {
                Drawing(list, "Empty archive symbol", Page == ScreenPage.Logs ? MasterComputerGraphic.Drawing.Disk : Page == ScreenPage.Tools ? MasterComputerGraphic.Drawing.Report : MasterComputerGraphic.Drawing.Clear, .33f, .38f, .34f, .21f, PageColor);
                var emptyLabel = Label(list, "Empty archive", Page == ScreenPage.Logs ? "READER STANDBY" : Page == ScreenPage.Tools ? "NO REPORTS FILED" : "NO MESSAGES", .06f, .64f, .88f, .10f, 24);
                emptyLabel.alignment = TextAlignmentOptions.Center;
                emptyLabel.color = muted;
            }
            previous = Button(list, "<", .045f, .875f, .16f, .09f, () => TurnList(-1));
            next = Button(list, ">", .795f, .875f, .16f, .09f, () => TurnList(1));
            pageNumber = Label(list, "List page", $"{listPage + 1}/{Mathf.Max(1, Mathf.CeilToInt(count / 6f))}", .25f, .875f, .50f, .09f, 23);
            pageNumber.alignment = TextAlignmentOptions.Center;
            previous.interactable = listPage > 0;
            next.interactable = (listPage + 1) * 6 < count;

            BuildRecordPanel();
            details = Label(detailContent, "Record body", "", .01f, 0, .97f, 1, 27);
            details.alignment = TextAlignmentOptions.TopLeft;
            if (selected >= 0 && selected < count) ShowRecord(selected);
            else
            {
                detailTitle.text = count == 0 ? (Page == ScreenPage.Alerts ? "NO SHIP ALERTS" : Page == ScreenPage.Logs ? "ARCHIVE EMPTY" : "AWAITING FIELD REPORTS") : "SELECT A RECORD";
                detailMeta.text = "";
                details.text = count > 0 ? "CHOOSE AN ENTRY ON THE LEFT." : Page == ScreenPage.Logs ? "INSERT A DISK IN THE CONSOLE READER TO ADD A LOG." : Page == ScreenPage.Tools ? "COLLECTED TOOL REPORTS WILL APPEAR HERE." : "NEW SHIP MESSAGES WILL APPEAR HERE.";
                if (Page == ScreenPage.Alerts && count == 0)
                {
                    recordSymbol.drawing = MasterComputerGraphic.Drawing.Clear;
                    recordSymbol.color = good;
                    recordSymbol.SetVerticesDirty();
                }
                FitDetails();
            }
        }

        private void BuildRecordPanel()
        {
            var right = Frame("Record details", pageRoot, .32f, 0, .68f, 1);
            Color accent = PageColor;
            recordAccent = TerminalUI.Panel("Record accent", right, 0, 0, 1, .008f, accent);
            recordStamp = Label(right, "Record stamp", "--", .74f, .04f, .22f, .065f, 23);
            recordStamp.alignment = TextAlignmentOptions.Right;
            recordStamp.color = accent;
            float bodyY;
            if (Page == ScreenPage.Logs)
            {
                // A document layout gives logs more reading room than the other pages.
                recordSection = Label(right, "Record section", "SHIPBOARD ARCHIVE / LOG", .04f, .04f, .69f, .065f, 24);
                detailTitle = Label(right, "Record title", "", .04f, .14f, .92f, .14f, 38);
                TerminalUI.Panel("Document metadata", right, .04f, .31f, .92f, .14f, panel);
                detailMeta = Label(right, "Record details", "", .055f, .315f, .89f, .13f, 23);
                bodyY = .50f;
                Label(right, "Document footer", "LOCAL ARCHIVE / READ ONLY", .04f, .94f, .92f, .045f, 18).color = muted;
            }
            else if (Page == ScreenPage.Tools)
            {
                recordSection = Label(right, "Record section", "RESEARCH / FIELD EQUIPMENT", .04f, .04f, .69f, .065f, 24);
                detailTitle = Label(right, "Record title", "", .04f, .14f, .92f, .14f, 38);
                var meta = TerminalUI.Panel("Equipment classification", right, .04f, .31f, .92f, .20f, panel);
                TerminalUI.Panel("Classification edge", meta.transform, 0, 0, .006f, 1, cyan);
                detailMeta = Label(meta.transform, "Record details", "", .025f, .12f, .95f, .76f, 25);
                Label(right, "Report heading", "OPERATING NOTES", .04f, .55f, .92f, .06f, 22).color = cyan;
                bodyY = .64f;
            }
            else
            {
                recordSection = Label(right, "Record section", "ALERT CHANNEL / SHIP SYSTEMS", .04f, .04f, .69f, .065f, 24);
                recordSymbol = Drawing(right, "Priority symbol", MasterComputerGraphic.Drawing.Caution, .045f, .16f, .085f, .13f, warning);
                detailTitle = Label(right, "Record title", "", .16f, .135f, .80f, .18f, 36);
                TerminalUI.Panel("Message priority", right, .04f, .35f, .92f, .10f, panel);
                detailMeta = Label(right, "Record details", "", .055f, .36f, .89f, .08f, 24);
                bodyY = .51f;
                Label(right, "Alert footer", "OPENING A MESSAGE MARKS IT READ", .04f, .94f, .92f, .045f, 19).color = muted;
            }
            recordSection.color = accent;
            detailTitle.color = accent;
            detailTitle.overflowMode = TextOverflowModes.Ellipsis;
            detailTitle.maxVisibleLines = 2;
            detailMeta.color = muted;
            detailScroll = TerminalUI.Scroll(right, .04f, bodyY, .92f, .91f - bodyY, out detailContent);
        }

        private MasterComputerRecord Record(int index) => Page == ScreenPage.Logs ? Data.Logs[index] : Data.Tools[index];
        private void TurnList(int direction) { listPage += direction; BuildPage(); }

        public void SelectRecord(int index)
        {
            if (!Ready || Transitioning || index < 0 || index >= Count || Page == ScreenPage.Status) return;
            selected = index;
            ShowRecord(index);
            for (int i = 0; i < recordMarks.Length; i++)
                if (recordMarks[i] != null) recordMarks[i].enabled = listPage * 6 + i == selected;
            if (Page == ScreenPage.Alerts) Data.ReadAlert(index);
        }

        private void ShowRecord(int index)
        {
            if (Page == ScreenPage.Alerts)
            {
                var alert = Data.Alerts[index];
                detailTitle.text = Upper(alert.title);
                detailTitle.color = alert.critical ? TerminalUI.Conflict : warning;
                detailMeta.text = alert.critical ? "PRIORITY / CRITICAL" : "PRIORITY / NOTICE";
                detailMeta.text += "    /    READ";
                detailMeta.color = detailTitle.color;
                recordAccent.color = recordSymbol.color = detailTitle.color;
                recordSymbol.drawing = alert.critical ? MasterComputerGraphic.Drawing.Caution : MasterComputerGraphic.Drawing.Report;
                recordSymbol.SetVerticesDirty();
                recordStamp.text = $"MSG {index + 1:000}";
                details.text = Upper(alert.body);
            }
            else
            {
                var record = Record(index);
                detailTitle.text = Upper(record.title);
                detailMeta.text = Upper(Page == ScreenPage.Logs ? "PERSONNEL: " + record.author + "\nRECORDED: " + record.recordedAt : "USE: " + record.useType + "\nFOUND ON: " + record.foundOn);
                recordStamp.text = (Page == ScreenPage.Logs ? "LOG " : "FILE ") + $"{index + 1:000}";
                details.text = Upper(record.body);
            }
            FitDetails();
        }

        private void FitDetails()
        {
            Canvas.ForceUpdateCanvases();
            float height = details.GetPreferredValues(details.text, Mathf.Max(1, detailScroll.viewport.rect.width - 12), 0).y + 30;
            detailContent.sizeDelta = new Vector2(0, Mathf.Max(height, detailScroll.viewport.rect.height));
            detailScroll.verticalNormalizedPosition = 1;
            // Hide an unneeded handle immediately, before the next ScrollRect layout update.
            detailScroll.verticalScrollbar.gameObject.SetActive(height > detailScroll.viewport.rect.height + .5f);
        }

        private static string Upper(string value) => (value ?? "").ToUpperInvariant();
        private void OnDataChanged() => dirty = true;

        private void Update()
        {
            if (controls == null || !Ready || Transitioning) return;
            if (dirty)
            {
                dirty = false;
                int currentUnread = Data == null ? 0 : Data.UnreadCount;
                if (Page != ScreenPage.Status && (Count != listCount || currentUnread != unread)) BuildPage();
                unread = currentUnread;
                RefreshGate();
            }
            if (Page == ScreenPage.Status) RefreshStatus();
        }

        private void RefreshStatus()
        {
            var game = Data == null ? null : Data.Game;
            if (game == null || game.Resources == null) return;
            var r = game.Resources;
            int hull = Mathf.RoundToInt(100f * r.HullPips / Mathf.Max(1, r.MaxHullPips));
            bool critical = r.HullPips <= Mathf.Max(1, r.MaxHullPips / 3);
            shipStatus.text = $"{hull}% / " + (critical ? "CRITICAL" : hull < 100 ? "DAMAGED" : "STABLE");
            shipStatus.color = critical ? TerminalUI.Conflict : hull < 100 ? warning : good;
            hullFill.color = shipDrawing.color = shipStatus.color;
            hullFill.rectTransform.anchorMax = new Vector2(hull / 100f, 1);
            hullCaption.text = critical ? "REPAIR REQUIRED" : hull < 100 ? "DAMAGE REGISTERED" : "INTEGRITY WITHIN OPERATING LIMITS";
            resourceValues[0].text = $"{r.NitrogenCanisters:00}";
            resourceValues[1].text = $"{r.ShipParts:00}";
            resourceValues[2].text = $"{r.ParanormalEnergy:0}";
            string progress = total > 0 ? $"{catalogued} / {total} CATALOGUED\n{discovered} / {total} DISCOVERED" : "AWAITING SCANNER DATA";
            currentSystem.text = $"CURRENT SYSTEM: {game.CurrentSystemNumber:00}";
            completion.text = game.CatalogSubmitted ? "SUBMITTED" : total > 0 ? $"{Mathf.RoundToInt(100f * catalogued / total)}% COMPLETE" : "SCANNING";
            surveyFill.rectTransform.anchorMax = new Vector2(total > 0 ? Mathf.Clamp01((float)catalogued / total) : 0, 1);
            survey.text = progress;
            catalogState.text = game.CatalogSubmitted ? "CATALOG ACCEPTED" : game.Phase == GameManager.GamePhase.SystemComplete ? "READY FOR SUBMISSION" : "COMPLETE REQUIRED SCANS TO SUBMIT";
            submit.interactable = InUse && !AlertBlocking && game.Phase == GameManager.GamePhase.SystemComplete && !game.CatalogSubmitted;
        }

        private void RefreshGate()
        {
            bool alert = Ready && !Transitioning && AlertBlocking;
            controls.interactable = controls.blocksRaycasts = Ready && InUse && !alert;
            // Exit stays available while page controls wait for their new content.
            pageFade.interactable = pageFade.blocksRaycasts = Ready && InUse && !Transitioning && !alert;
            warningRoot.parent.gameObject.SetActive(alert);
            reviewAlert.interactable = InUse;
            if (alert) alertText.text = $"{Data.UnreadCount} UNREAD SHIP " + (Data.UnreadCount == 1 ? "ALERT" : "ALERTS");
            if (alert && InUse && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(reviewAlert.gameObject);
        }

        private void OnDestroy()
        {
            EndIntrusion();
            if (Data != null) Data.Changed -= OnDataChanged;
        }
    }
}
