using Foldspace.Controls;
using Foldspace.Rendering;
using Foldspace.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Foldspace.UI
{
    /// <summary>The in-run HUD in the screen corners, outside the lens: hull and dash, score and time, level and XP, fold chain.</summary>
    public class RunHudView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;

        [Header("Hull")]
        [SerializeField] Image[] pips;
        [SerializeField] Image[] pipGlows;

        [Header("Dash")]
        [SerializeField] Image dashFill;
        [SerializeField] Text dashState;

        [Header("Score")]
        [SerializeField] Text score;
        [SerializeField] Text best;
        [SerializeField] Text time;

        [Header("Level")]
        [SerializeField] RectTransform levelBadge;
        [SerializeField] Text level;
        [SerializeField] Text xp;
        [SerializeField] Image xpFill;

        [Header("Chain")]
        [SerializeField] CanvasGroup chainGroup;
        [SerializeField] Text chain;
        [SerializeField] Image chainFill;

        [Header("Steering")]
        [SerializeField] Text steering;

        GameContext context;
        float[] pipPunch;
        int shownHull;
        float dashPunch;
        bool dashWasReady;
        int dashTenths = -1;
        float shownScore;
        int shownScoreInt = -1;
        float scorePunch;
        int lastScore;
        int shownBest = -1;
        int shownSeconds = -1;
        float levelPunch;
        int lastLevel = -1;
        int lastXpKey = -1;
        float chainPunch;
        int lastChain;
        int shownSteer = int.MinValue;

        public CanvasGroup Group => group;

        public void Init(GameContext ctx)
        {
            context = ctx;
            pipPunch = new float[pips.Length];
            UiMotion.Hide(group);
            UiMotion.Hide(chainGroup);
        }

        public void ResetRun()
        {
            shownHull = context.Player != null ? context.Player.Hull : 0;
            System.Array.Clear(pipPunch, 0, pipPunch.Length);
            shownScore = 0f;
            shownScoreInt = -1;
            lastScore = 0;
            scorePunch = 0f;
            lastLevel = -1;
            lastXpKey = -1;
            lastChain = 0;
            dashWasReady = true;
        }

        public void Tick(float now, float dt)
        {
            var player = context.Player;
            if (player == null) return;
            UpdateHull(player.Hull, now, dt);
            UpdateDash(player.DashReadyIn, dt);
            UpdateScore(dt);
            UpdateLevel(dt);
            UpdateChain(dt);
            UpdateSteering();
        }

        void UpdateHull(int hull, float now, float dt)
        {
            int count = Mathf.Min(context.Config.ship.hullPips, pips.Length);
            for (int i = Mathf.Max(0, hull); i < Mathf.Min(shownHull, pips.Length); i++) pipPunch[i] = 1f;
            shownHull = hull;
            for (int i = 0; i < pips.Length; i++)
            {
                bool used = i < count;
                if (pips[i].gameObject.activeSelf != used)
                {
                    pips[i].gameObject.SetActive(used);
                    pipGlows[i].gameObject.SetActive(used);
                }
                if (!used) continue;

                pipPunch[i] = Mathf.MoveTowards(pipPunch[i], 0f, dt * 2.5f);
                float punch = pipPunch[i];
                bool intact = i < hull;
                Color c = intact ? Palette.Repair : new Color(1f, 1f, 1f, 0.14f);
                if (intact && hull == 1) c = Color.Lerp(Palette.Repair, Palette.Danger, 0.5f + 0.5f * Mathf.Sin(now * 9f));
                pips[i].color = Color.Lerp(c, Palette.Danger, punch);
                pips[i].rectTransform.localScale = Vector3.one * (1f + 0.7f * punch);
                pips[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 70f) * 18f * punch);
                pipGlows[i].color = intact
                    ? (hull == 1 ? Palette.Danger : Palette.Repair).WithAlpha(0.3f)
                    : Palette.Danger.WithAlpha(0.5f * punch);
            }
        }

        void UpdateDash(float readyIn, float dt)
        {
            bool ready = readyIn <= 0f;
            if (ready && !dashWasReady) dashPunch = 1f;
            dashWasReady = ready;
            dashPunch = Mathf.MoveTowards(dashPunch, 0f, dt * 3f);

            UiMotion.SetFill(dashFill, ready ? 1f : 1f - readyIn / Mathf.Max(0.01f, context.Config.dash.cooldown));
            dashFill.color = ready ? Color.Lerp(Palette.You, Color.white, dashPunch) : new Color(1f, 1f, 1f, 0.4f);
            dashFill.rectTransform.parent.localScale = new Vector3(1f, 1f + dashPunch, 1f);

            int tenths = ready ? -2 : Mathf.CeilToInt(readyIn * 10f);
            if (tenths != dashTenths)
            {
                dashTenths = tenths;
                dashState.text = ready ? "READY" : (tenths / 10f).ToString("0.0") + "s";
            }
            dashState.color = ready ? Palette.You : Palette.TextDim;
        }

        void UpdateScore(float dt)
        {
            var session = context.Session;
            int value = session.Score;
            if (value > lastScore) scorePunch = Mathf.Min(1f, scorePunch + 0.35f + (value - lastScore) / 200f);
            lastScore = value;
            shownScore = Mathf.Lerp(shownScore, value, 1f - Mathf.Exp(-12f * dt));
            if (value - shownScore < 0.5f) shownScore = value;
            int rounded = Mathf.RoundToInt(shownScore);
            if (rounded != shownScoreInt)
            {
                shownScoreInt = rounded;
                score.text = rounded.ToString("N0");
            }
            scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 3f);
            score.rectTransform.localScale = Vector3.one * (1f + 0.22f * scorePunch);
            score.color = Color.Lerp(Color.white, Palette.You, scorePunch);

            int bestScore = Mathf.Max(context.HighScores.Best, value);
            if (bestScore != shownBest)
            {
                shownBest = bestScore;
                best.text = "BEST  " + bestScore.ToString("N0");
            }
            int seconds = Mathf.FloorToInt(session.RunTime);
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                time.text = Format.Clock(session.RunTime);
            }
        }

        void UpdateLevel(float dt)
        {
            var session = context.Session;
            if (session.Level != lastLevel)
            {
                if (lastLevel > 0 && session.Level > lastLevel) levelPunch = 1f;
                lastLevel = session.Level;
                level.text = session.Level.ToString();
            }
            int key = session.Xp * 10000 + session.XpToNext;
            if (key != lastXpKey)
            {
                lastXpKey = key;
                xp.text = $"{session.Xp} / {session.XpToNext}";
                UiMotion.SetFill(xpFill, session.XpFraction);
            }
            levelPunch = Mathf.MoveTowards(levelPunch, 0f, dt * 2.5f);
            levelBadge.localScale = Vector3.one * (1f + 0.35f * Ease.OutBack(levelPunch));
        }

        void UpdateChain(float dt)
        {
            var session = context.Session;
            float remaining = session.ChainRemaining(Time.time);
            bool show = context.IsPlaying && session.Chain > 0 && remaining > 0f;
            UiMotion.Fade(chainGroup, show ? 1f : 0f, dt, show ? 12f : 3f);
            if (session.Chain != lastChain)
            {
                if (session.Chain > lastChain) chainPunch = 1f;
                lastChain = session.Chain;
                if (lastChain > 0) chain.text = "×" + lastChain;
            }
            chainPunch = Mathf.MoveTowards(chainPunch, 0f, dt * 3.5f);
            Color c = Palette.ChainColor(Mathf.Max(1, lastChain));
            chain.color = Color.Lerp(c, Color.white, chainPunch * 0.6f);
            chain.rectTransform.localScale = Vector3.one * (1f + 0.45f * chainPunch);
            chainFill.color = c;
            if (show) UiMotion.SetFill(chainFill, remaining);
        }

        void UpdateSteering()
        {
            var input = context.Input;
            int mode = input.IsTouch ? -1 : (int)input.SteerMode;
            if (mode == shownSteer) return;
            shownSteer = mode;
            steering.text = mode < 0 ? "" : input.SteerMode switch
            {
                SteerMode.FollowCursor => "MOUSE: SHIP FOLLOWS THE CURSOR   ·   TAB TO SWITCH",
                SteerMode.DragStick => "MOUSE: HOLD AND DRAG LIKE A STICK   ·   TAB TO SWITCH",
                _ => "KEYBOARD ONLY   ·   TAB TO SWITCH",
            };
        }
    }
}
