using UnityEngine;
using UnityEngine.UI;

namespace CubeBlaster
{
    public class SettingsScreen : UIScreen
    {
        Text _muteLabel;

        public SettingsScreen(IUIHost ui) : base(ui) { }

        public override void Build(RectTransform parent)
        {
            Root = MakeRoot("Settings", parent);
            var t = Root.transform;
            var pal = PaletteConfig.Active;

            var dim = UIFactory.Panel("Dim", t, new Color(0, 0, 0, 0.5f));
            var dimImage = dim.GetComponent<Image>();
            dimImage.sprite = null;
            dimImage.color = new Color(0, 0, 0, 0.5f);

            var card = UIFactory.Card("Card", t, pal.cardWhite);
            UIFactory.Rect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380, -360), new Vector2(380, 360));
            card.AddComponent<UIPopIn>().from = 0.75f;

            UIFactory.Ribbon(card.transform, "OPTIONS", pal.btnBlue, 480f, 56);

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
            AudioService.Current.PlayClick();
            Hide();
        }

        void ToggleMute()
        {
            AudioService.Current.PlayClick();
            AudioService.Current.SetMuted(!SaveSystem.Muted);
            Refresh();
        }

        void ResetProgress()
        {
            AudioService.Current.PlayClick();
            SaveSystem.ClearAll();
            Refresh();
        }

        public override void Refresh()
        {
            if (_muteLabel != null) _muteLabel.text = SaveSystem.Muted ? "SOUND: OFF" : "SOUND: ON";
        }
    }
}
