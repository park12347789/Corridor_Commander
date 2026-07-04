using CorridorCommander.PlayerControl;
using UnityEngine;

namespace CorridorCommander.PlayerUI
{
    [DisallowMultipleComponent]
    public sealed class PlayerSquadSelectionWorldPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerSquadRoster roster;

        private readonly AlliedSquadMemberFollower[] boundMembers =
            new AlliedSquadMemberFollower[PlayerSquadRoster.MaxMemberCount];
        private readonly SquadSelectionIndicator[] indicators =
            new SquadSelectionIndicator[PlayerSquadRoster.MaxMemberCount];

        private PlayerSquadRoster subscribedRoster;

        private void Awake()
        {
            ResolveRoster();
        }

        private void OnEnable()
        {
            ResolveRoster();
            Subscribe();
            RefreshIndicators();
        }

        private void OnDisable()
        {
            Unsubscribe();
            HideAllIndicators();
        }

        private void Update()
        {
            if (roster == null)
            {
                ResolveRoster();
                Subscribe();
            }

            RefreshIndicators();
        }

        private void Subscribe()
        {
            if (roster == null || subscribedRoster == roster)
            {
                return;
            }

            Unsubscribe();
            subscribedRoster = roster;
            subscribedRoster.RosterChanged += RefreshIndicators;
            subscribedRoster.SelectionChanged += RefreshIndicators;
        }

        private void Unsubscribe()
        {
            if (subscribedRoster == null)
            {
                return;
            }

            subscribedRoster.RosterChanged -= RefreshIndicators;
            subscribedRoster.SelectionChanged -= RefreshIndicators;
            subscribedRoster = null;
        }

        private void RefreshIndicators()
        {
            if (roster == null)
            {
                return;
            }

            for (int i = 0; i < indicators.Length; i++)
            {
                AlliedSquadMemberFollower member = roster.GetMemberAt(i);
                if (boundMembers[i] != member)
                {
                    ClearIndicator(i);
                    boundMembers[i] = member;
                    indicators[i] = ResolveIndicator(member);
                }

                if (indicators[i] != null)
                {
                    indicators[i].SetSelected(roster.IsSlotSelected(i), roster.IsAllSelected);
                }
            }
        }

        private void HideAllIndicators()
        {
            for (int i = 0; i < indicators.Length; i++)
            {
                ClearIndicator(i);
            }
        }

        private void ClearIndicator(int index)
        {
            SquadSelectionIndicator indicator = indicators[index];
            if (indicator != null)
            {
                indicator.SetSelected(false, false);
            }

            indicators[index] = null;
        }

        private static SquadSelectionIndicator ResolveIndicator(AlliedSquadMemberFollower member)
        {
            if (member == null)
            {
                return null;
            }

            SquadSelectionIndicator indicator = member.GetComponent<SquadSelectionIndicator>();
            return indicator != null ? indicator : member.gameObject.AddComponent<SquadSelectionIndicator>();
        }

        private void ResolveRoster()
        {
            if (roster == null)
            {
                roster = GetComponent<PlayerSquadRoster>();
            }

            if (roster == null)
            {
                roster = PlayerSquadRoster.Instance;
            }
        }
    }
}
