using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Odyssey
{
    internal sealed class OracleChat : MonoBehaviour
    {
        private const int MaxQuestionLength = 280;
        private const string Greeting = "Speak, wanderer. Ask what thou wilt of the rules of thy voyage.";
        private const string Unavailable = "The winds have silenced me. Try again in a moment.";
        private const string Unconfigured = "The Oracle is unavailable. The game has no Oracle server configured.";

        [Serializable]
        private sealed class Question
        {
            public string message;
            public string context;
        }

        [Serializable]
        private sealed class Reply
        {
            public string reply = "";
            public string context = "";
        }

        private struct Line
        {
            internal string Text;
            internal bool IsPlayer;
        }

        private static readonly List<Line> transcript = new List<Line>();
        private static string context = "";
        private static string draft = "";

        private RectTransform panel;
        private RectTransform history;
        private ScrollRect scroll;
        private InputField input;
        private Button send;
        private Text status;
        private UnityWebRequest pending;
        private string pendingQuestion;
        private bool scrollToLatest;
        private bool visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetConversation()
        {
            transcript.Clear();
            context = "";
            draft = "";
        }

        internal void Build(RectTransform parent)
        {
            panel = OdysseyTheme.Rect(parent, "Oracle chat", Vector2.zero, Vector2.one,
                new Vector2(16, 14), new Vector2(-16, -84));
            var scrollArea = OdysseyTheme.Rect(panel, "Conversation", Vector2.zero, Vector2.one,
                new Vector2(0, 78), Vector2.zero);
            scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = OdysseyTheme.Inset(scrollArea, "Viewport", 0);
            OdysseyTheme.Tint(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();

            history = OdysseyTheme.Rect(viewport, "Messages", Vector2.up, Vector2.one,
                Vector2.zero, Vector2.zero);
            history.pivot = new Vector2(0.5f, 1f);
            var layout = history.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(0, 0, 0, 4);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fit = history.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = history;
            scroll.viewport = viewport;

            status = OdysseyTheme.Label(OdysseyTheme.Rect(panel, "Oracle status", Vector2.zero, Vector2.right,
                new Vector2(0, 52), new Vector2(0, 77)), "", 13, OdysseyTheme.Muted, TextAnchor.MiddleLeft);
            var field = OdysseyTheme.Rect(panel, "Question", Vector2.zero, Vector2.right,
                Vector2.zero, new Vector2(-78, 48));
            var background = OdysseyTheme.Tint(field, OdysseyTheme.DeepSea, true);
            OdysseyTheme.Frame(field, OdysseyTheme.Bronze, 1);
            var text = OdysseyTheme.Label(OdysseyTheme.Inset(field, "Question text", 9),
                "", 17, OdysseyTheme.Ivory, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            var placeholder = OdysseyTheme.Label(OdysseyTheme.Inset(field, "Question prompt", 9),
                "Ask about the rules", 15, OdysseyTheme.Muted, TextAnchor.MiddleLeft);
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = MaxQuestionLength;
            // Unity 2019's hidden iOS input can restore an old caret position while typing.
            input.shouldHideMobileInput = false;
            input.text = draft;
            input.onValueChanged.AddListener(value => { draft = value; UpdateSend(); });
            input.onEndEdit.AddListener(_ =>
            {
                if (visible && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || TouchScreenKeyboard.Status.Done == (input.touchScreenKeyboard?.status)))
                    SendQuestion();
            });

            var action = OdysseyTheme.Rect(panel, "Send", Vector2.right, Vector2.right,
                new Vector2(-70, 0), new Vector2(0, 48));
            send = OdysseyTheme.Button(action, SendQuestion, true);
            OdysseyTheme.Label(OdysseyTheme.Inset(action, "Label", 4), "Send", 17, OdysseyTheme.Ink);

            AddBubble(new Line { Text = Greeting });
            foreach (var line in transcript)
                AddBubble(line);
            UpdateSend();
            SetVisible(false);
        }

        internal void SetVisible(bool value)
        {
            if (visible == value && panel.gameObject.activeSelf == value)
                return;
            visible = value;
            if (!visible)
                input.DeactivateInputField();
            panel.gameObject.SetActive(visible);
            scrollToLatest = visible;
        }

        private void UpdateSend()
        {
            if (send != null)
                send.interactable = pending == null && !string.IsNullOrWhiteSpace(input.text);
        }

        private void SendQuestion()
        {
            if (!visible || pending != null || string.IsNullOrWhiteSpace(input.text))
                return;

            string endpoint = FindEndpoint();
            if (endpoint == null)
            {
                status.text = Unconfigured;
                return;
            }
            string message = input.text.Trim();
            var payload = new Question { message = message, context = context };
            pendingQuestion = message;
            pending = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
            pending.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
            pending.downloadHandler = new DownloadHandlerBuffer();
            pending.SetRequestHeader("Content-Type", "application/json");
            pending.timeout = 13;
            pending.redirectLimit = 0;
            AddLine(message, true);
            input.text = "";
            input.DeactivateInputField();
            status.text = "The Oracle is listening…";
            UpdateSend();
            StartCoroutine(Consult(pending, message));
        }

        private IEnumerator Consult(UnityWebRequest request, string message)
        {
            yield return request.SendWebRequest();

            string response = null;
            if (!request.isNetworkError && !request.isHttpError)
            {
                try
                {
                    var result = JsonUtility.FromJson<Reply>(request.downloadHandler.text);
                    if (result != null && !string.IsNullOrWhiteSpace(result.reply) && result.reply.Length <= 500)
                    {
                        response = result.reply;
                        context = result.context ?? "";
                    }
                }
                catch (ArgumentException)
                {
                    // Invalid proxy responses take the same retry path as an unavailable server.
                }
            }

            long code = request.responseCode;
            pending = null;
            pendingQuestion = null;
            request.Dispose();
            if (response != null)
            {
                status.text = "";
                AddLine(response, false);
            }
            else
            {
                status.text = code == 429 ? "The Oracle is busy. Try again shortly." : Unavailable;
                if (string.IsNullOrWhiteSpace(input.text))
                    input.text = message;
            }
            UpdateSend();
        }

        private void AddLine(string message, bool player)
        {
            var line = new Line { Text = message, IsPlayer = player };
            transcript.Add(line);
            AddBubble(line);
            scrollToLatest = true;
        }

        private void AddBubble(Line line)
        {
            var bubble = OdysseyTheme.Inset(history, line.IsPlayer ? "Player message" : "Oracle message", 0);
            OdysseyTheme.Tint(bubble, line.IsPlayer ? OdysseyTheme.DeepSea : (Color)new Color32(55, 48, 36, 255));
            var column = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(12, 12, 8, 10);
            column.spacing = 4;
            column.childControlHeight = column.childControlWidth = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            OdysseyTheme.Label(OdysseyTheme.Inset(bubble, "Speaker", 0), line.IsPlayer ? "You" : "Oracle",
                13, OdysseyTheme.Gold, TextAnchor.MiddleLeft);
            var body = OdysseyTheme.Label(OdysseyTheme.Inset(bubble, "Message", 0), line.Text,
                17, OdysseyTheme.Ivory, TextAnchor.UpperLeft);
            body.supportRichText = false;
            body.lineSpacing = 1.1f;
        }

        private void LateUpdate()
        {
            if (!visible || !scrollToLatest)
                return;
            scrollToLatest = false;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;
        }

        private void OnDestroy()
        {
            if (pending == null)
                return;
            if (string.IsNullOrWhiteSpace(draft))
                draft = pendingQuestion;
            pending.Abort();
            pending.Dispose();
            pending = null;
        }

        private static string FindEndpoint()
        {
            string url = Services.Config?.serverUrl;
#if UNITY_EDITOR
            var editorUrl = Environment.GetEnvironmentVariable("ARDYSSEY_ORACLE_URL");
            if (!string.IsNullOrWhiteSpace(editorUrl))
                url = editorUrl;
#endif
            if (string.IsNullOrWhiteSpace(url))
                url = Resources.Load<TextAsset>("Odyssey/OracleEndpoint")?.text;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim().TrimEnd('/') + "/oracle", UriKind.Absolute, out Uri uri)
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return null;
            if (uri.Scheme == Uri.UriSchemeHttps)
                return uri.AbsoluteUri;
#if UNITY_EDITOR
            if (uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)
                return uri.AbsoluteUri;
#endif
            return null;
        }
    }
}
