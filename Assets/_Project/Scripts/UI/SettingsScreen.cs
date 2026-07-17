using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class SettingsScreen : UIScreen
    {
        Text _muteLabel;

        public SettingsScreen(UIController ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Settings", parent);
            var t = Root.transform;
            var pal = Palette.Active;

            var dim = UIFactory.Panel("Dim", t, new Color(0, 0, 0, 0.5f));
            var dimImg = dim.GetComponent<Image>();
            dimImg.sprite = null; dimImg.color = new Color(0, 0, 0, 0.5f);

            var card = UIFactory.Card("Card", t, pal.cardWhite);
            UIFactory.Rect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380, -360), new Vector2(380, 360));
            var pop = card.AddComponent<UIPopIn>();
            pop.from = 0.75f;

            UIFactory.Ribbon(card.transform, "OPTIONS", pal.btnBlue, 480f, 56);

            // Non-empty initial label so the Text child exists (empty label = icon button).
            var mute = UIFactory.CandyButton("Mute", card.transform, "SOUND: ON", pal.btnBlue, ToggleMute, 44);
            UIFactory.Rect(mute.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, 40), new Vector2(300, 170));
            _muteLabel = mute.GetComponentInChildren<Text>();

            var reset = UIFactory.CandyButton("Reset", card.transform, "RESET PROGRESS", pal.btnRed, ResetProgress, 40);
            UIFactory.Rect(reset.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -120), new Vector2(300, 10));

            var close = UIFactory.CandyButton("Close", card.transform, "CLOSE", pal.btnSlate, Close, 44);
            UIFactory.Rect(close.gameObject, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, 50), new Vector2(260, 180));
        }

        void Close()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            Hide();
        }

        void ToggleMute()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            if (AudioManager.Instance != null) AudioManager.Instance.SetMuted(!SaveSystem.Muted);
            else SaveSystem.Muted = !SaveSystem.Muted;
            Refresh();
        }

        void ResetProgress()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            SaveSystem.ClearAll();
            Refresh();
        }

        public override void Refresh()
        {
            if (_muteLabel != null) _muteLabel.text = SaveSystem.Muted ? "SOUND: OFF" : "SOUND: ON";
        }
    }
}
