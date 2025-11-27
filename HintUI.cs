using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketCartPlus
{
    public class HintUI : SemiUI
    {
        public TextMeshProUGUI Text = null!;
        internal static HintUI instance = null!;
        public string messagePrev = "prev";
        public float messageTimer;
        public Color textColor;
        internal Transform normalParent = null!;
        internal bool grabHint = false;

        public override void Start()
        {
            base.Start();
            Text = GetComponent<TextMeshProUGUI>();
            if(Text == null)
            {
                Plugin.Log.LogError("NULL HINTER TEXT!!");
                return;
            }

            instance = this; 
            Text.text = "";
            normalParent = transform.parent;
            showPosition.x = 0f;
            showPosition.y = 0f;
            textColor = new(0, 102, 0);
            hidePosition.x = 0f;
            hidePosition.y = -30f;
        }

        public void ShowInfo(string message, Color color, float fontSize)
        {
            if (messageTimer <= 0f)
            {
                messageTimer = 0.2f;
                if (message != messagePrev)
                {
                    Text.text = message;
                    SemiUISpringShakeY(20f, 10f, 0.3f);
                    SemiUISpringScale(0.4f, 5f, 0.2f);
                    textColor = color;
                    Text.fontSize = fontSize;
                    ((Graphic)Text).color = textColor;
                    messagePrev = message;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (SemiFunc.RunIsShop())
                return;

            ItemInfoUI.instance.SemiUIScoot(new Vector2(0f, 8f));

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
            }
            else
            {
                ((Graphic)Text).color = Color.white;
                messagePrev = "prev";
                Hide();
            }
        }
    }
}
