using UnityEngine;

namespace ValalaMods
{
    internal sealed class WindChargeHud : MonoBehaviour
    {
        private const float BaseWidth = 260f;
        private const float BaseHeight = 10f;
        private const float BaseBottom = 170f;

        private void OnGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            if (!StaffFreeWindImpulse.IsCharging)
                return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2f);
            float width = BaseWidth * scale;
            float height = BaseHeight * scale;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - BaseBottom * scale;

            DrawRect(new Rect(x, y, width, height), new Color(0.03f, 0.04f, 0.06f, 0.82f));
            DrawRect(new Rect(x, y, width, 1f * scale), new Color(0.35f, 0.45f, 0.55f, 0.55f));

            float fill = StaffFreeWindImpulse.ChargeFraction;
            DrawRect(new Rect(x, y, width * fill, height), new Color(0.55f, 0.88f, 1f, 0.92f));

            float limit = StaffFreeWindImpulse.LimitFraction;
            DrawRect(new Rect(x + width * limit - 1f * scale, y - 2f * scale, 2f * scale, height + 4f * scale),
                new Color(1f, 0.82f, 0.3f, 0.95f));

            for (int i = 1; i < 4; i++)
            {
                float tick = i / 4f;
                DrawRect(new Rect(x + width * tick - 1f * scale, y, 1f * scale, height),
                    new Color(0.04f, 0.06f, 0.08f, 0.55f));
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
