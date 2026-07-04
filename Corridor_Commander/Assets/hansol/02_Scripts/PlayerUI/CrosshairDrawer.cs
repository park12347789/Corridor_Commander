using UnityEngine;
using CorridorCommander;

namespace CorridorCommander.PlayerUI
{
    public sealed class CrosshairDrawer : MonoBehaviour
    {
        [SerializeField] private float size = 8f;
        [SerializeField] private float thickness = 2f;
        [SerializeField] private float gap = 4f;
        [SerializeField] private Color color = Color.white;

        private Texture2D whiteTexture;

        private void Awake()
        {
            whiteTexture = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            if (UiInputCoordinator.HasActiveContext
                || Cursor.visible
                || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            DrawCrosshair(centerX, centerY);

            GUI.color = previousColor;
        }

        private void DrawCrosshair(float centerX, float centerY)
        {
            // ���� ��
            GUI.DrawTexture(
                new Rect(
                    centerX - thickness * 0.5f,
                    centerY - gap - size,
                    thickness,
                    size
                ),
                whiteTexture
            );

            // �Ʒ��� ��
            GUI.DrawTexture(
                new Rect(
                    centerX - thickness * 0.5f,
                    centerY + gap,
                    thickness,
                    size
                ),
                whiteTexture
            );

            // ���� ��
            GUI.DrawTexture(
                new Rect(
                    centerX - gap - size,
                    centerY - thickness * 0.5f,
                    size,
                    thickness
                ),
                whiteTexture
            );

            // ������ ��
            GUI.DrawTexture(
                new Rect(
                    centerX + gap,
                    centerY - thickness * 0.5f,
                    size,
                    thickness
                ),
                whiteTexture
            );
        }
    }
}