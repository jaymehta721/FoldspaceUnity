using System;
using Foldspace.Utilities;
using UnityEngine;

namespace Foldspace.Controls
{
    public enum SteerMode
    {
        FollowCursor,
        DragStick,
        KeyboardOnly,
    }

    /// <summary>One frame of steering: either a heading to turn toward, or a turn rate (1 = full left, -1 = full right).</summary>
    public readonly struct SteerCommand
    {
        public readonly bool HasTarget;
        public readonly float TargetHeading;
        public readonly float Turn;
        public readonly bool Dash;

        SteerCommand(bool hasTarget, float target, float turn, bool dash)
        {
            HasTarget = hasTarget;
            TargetHeading = target;
            Turn = turn;
            Dash = dash;
        }

        public static SteerCommand Toward(float heading, bool dash) => new SteerCommand(true, heading, 0f, dash);
        public static SteerCommand Turning(float turn, bool dash) => new SteerCommand(false, 0f, Mathf.Clamp(turn, -1f, 1f), dash);
    }

    /// <summary>
    /// Turns keyboard, mouse and touch into game intents. Everything that reads the legacy Input Manager lives here,
    /// so moving to the Input System later touches one file.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        const float CursorDeadZone = 0.35f;
        const float StickDeadZoneInches = 0.08f;
        const float StickTravelInches = 0.5f;

        [Tooltip("How a mouse steers the ship. Press Tab in play mode to cycle.")]
        [SerializeField] SteerMode steerMode = SteerMode.FollowCursor;

        Camera worldCamera;
        int stickFinger = -1;
        Vector2 stickOrigin;
        Vector2 mouseStickOrigin;
        bool mouseStickActive;
        bool touchSeen;

        public SteerMode SteerMode => steerMode;
        public bool IsTouch => touchSeen || Application.isMobilePlatform;

        public void Init(Camera camera) => worldCamera = camera;

        void Update()
        {
            if (Input.touchCount > 0) touchSeen = true;
            if (Input.GetKeyDown(KeyCode.Tab))
                steerMode = (SteerMode)(((int)steerMode + 1) % Enum.GetValues(typeof(SteerMode)).Length);
        }

        public bool LaunchPressed()
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) return true;
            if (Input.GetMouseButtonDown(0)) return true;
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
            return false;
        }

        public bool PausePressed() => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);

        /// <summary>Reads this frame's steering for a ship at <paramref name="position"/> facing <paramref name="heading"/>. Call once per frame.</summary>
        public SteerCommand ReadSteer(Vector2 position, float heading)
        {
            bool dash = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1);
            float turn = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn += 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn -= 1f;

            float target = heading;
            bool hasTarget = false;
            if (IsTouch) hasTarget = TouchStick(heading, ref dash, out target);
            else if (turn == 0f) hasTarget = MouseSteer(position, heading, out target);

            return hasTarget && turn == 0f ? SteerCommand.Toward(target, dash) : SteerCommand.Turning(turn, dash);
        }

        /// <summary>First finger is a floating stick; any other finger that lands dashes.</summary>
        bool TouchStick(float heading, ref bool dash, out float target)
        {
            target = heading;
            bool stickHeld = false;
            Vector2 stickPosition = default;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    if (stickFinger < 0)
                    {
                        stickFinger = touch.fingerId;
                        stickOrigin = touch.position;
                    }
                    else if (touch.fingerId != stickFinger)
                    {
                        dash = true;
                    }
                }
                if (touch.fingerId != stickFinger) continue;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    stickFinger = -1;
                    continue;
                }
                stickHeld = true;
                stickPosition = touch.position;
            }
            return stickHeld && StickHeading(ref stickOrigin, stickPosition, heading, out target);
        }

        bool MouseSteer(Vector2 position, float heading, out float target)
        {
            target = heading;
            Vector2 mouse = Input.mousePosition;
            switch (steerMode)
            {
                case SteerMode.DragStick:
                    if (Input.GetMouseButtonDown(0))
                    {
                        mouseStickActive = true;
                        mouseStickOrigin = mouse;
                    }
                    if (Input.GetMouseButtonUp(0)) mouseStickActive = false;
                    return mouseStickActive && StickHeading(ref mouseStickOrigin, mouse, heading, out target);

                case SteerMode.FollowCursor:
                    if (worldCamera == null || mouse.x < 0f || mouse.y < 0f || mouse.x > Screen.width || mouse.y > Screen.height) return false;
                    Vector2 toCursor = (Vector2)worldCamera.ScreenToWorldPoint(mouse) - position;
                    if (toCursor.sqrMagnitude < CursorDeadZone * CursorDeadZone) return false;
                    target = Geometry.HeadingOf(toCursor);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Floating virtual stick: the origin trails the finger so the stick never runs out of travel.</summary>
        static bool StickHeading(ref Vector2 origin, Vector2 current, float heading, out float target)
        {
            target = heading;
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            float deadZone = dpi * StickDeadZoneInches;
            float maxTravel = dpi * StickTravelInches;
            Vector2 drag = current - origin;
            if (drag.magnitude > maxTravel)
            {
                origin = current - drag.normalized * maxTravel;
                drag = current - origin;
            }
            if (drag.magnitude < deadZone) return false;
            target = Geometry.HeadingOf(drag);
            return true;
        }
    }
}
