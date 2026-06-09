using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ImprovedPublicTransport2.OptionsFramework;
using UnityEngine;

namespace ImprovedPublicTransport2.Util
{
    /// <summary>
    /// Ticket-price happiness consequence, per the plan, done the RIGHT way: we never touch wellbeing
    /// ourselves. Instead we bump the INTERNAL tax rate the game reads while it computes a consequence,
    /// and let vanilla derive the happiness from it.
    ///
    ///   effectiveRate = realTaxRate + fareBurden / taxBase
    ///
    /// where fareBurden is the sum of (paid − type default) for the building's residents this period and
    /// taxBase is the building's REAL accumulated resident income (CommonBuildingAI.GetHomeBehaviour,
    /// m_incomeAccumulation). The rate delta is exposed via <see cref="ActiveRateDelta"/>, which is set
    /// only between <see cref="BeginConsequence"/> / <see cref="EndConsequence"/> (wrapping ResidentAI.
    /// UpdateWellbeing). GetTaxRatePatch adds it to GetTaxRate's result, so vanilla's own wellbeing term
    /// runs on the higher/lower rate. Everywhere else (income, budget, UI) GetTaxRate is untouched, so
    /// the player's configured rate is never changed.
    /// </summary>
    public static class TicketHappinessUtil
    {
        // home building -> fare premium (game units) accumulated this period
        private static readonly Dictionary<ushort, long> _burden = new Dictionary<ushort, long>();
        // home building -> cached fare rate delta for this period (income read at most once/building)
        private static readonly Dictionary<ushort, float> _rateDeltaCache = new Dictionary<ushort, float>();
        private static int _period = int.MinValue;
        private const int MaxTrackedBuildings = 32768;

        private static MethodInfo _getHomeBehaviour;
        private static int _boardingLogBudget = 25; // bounded diagnostics
        private static int _rateLogBudget = 40;

        /// <summary>
        /// Tax-rate bump currently in effect for GetTaxRate. Non-zero ONLY while the game is computing a
        /// consequence for a specific building (between BeginConsequence/EndConsequence). 0 the rest of
        /// the time, so income / budget / UI read the real player rate.
        /// </summary>
        public static int ActiveRateDelta;

        public static bool Enabled
        {
            get
            {
                try { return OptionsWrapper<Settings.Settings>.Options.TicketPriceHappinessEffect; }
                catch { return false; }
            }
        }

        private static int CurrentPeriod()
            => (int) (Singleton<SimulationManager>.instance.m_currentFrameIndex >> 17);

        private static void EnsurePeriod()
        {
            int p = CurrentPeriod();
            if (p == _period)
                return;
            _period = p;
            _burden.Clear();
            _rateDeltaCache.Clear();
        }

        /// <summary>Add one boarding's fare premium (paid − type default) to the rider's home building.</summary>
        public static void RecordBoarding(ushort homeBuilding, int premium)
        {
            if (homeBuilding == 0 || premium == 0 || !Enabled)
                return;
            EnsurePeriod();
            long cur;
            if (!_burden.TryGetValue(homeBuilding, out cur) && _burden.Count >= MaxTrackedBuildings)
                return;
            _burden[homeBuilding] = cur + premium;
            _rateDeltaCache.Remove(homeBuilding); // burden changed → recompute the rate delta
            if (_boardingLogBudget > 0)
            {
                _boardingLogBudget--;
                Log.Info("TicketHappiness: boarding home=" + homeBuilding + " premium=" + premium +
                         " buildingBurden=" + _burden[homeBuilding]);
            }
        }

        /// <summary>Begin a vanilla consequence calc for one building: activate its fare rate bump.</summary>
        public static void BeginConsequence(ushort buildingID)
        {
            ActiveRateDelta = (!Enabled || buildingID == 0) ? 0 : Mathf.RoundToInt(RateDelta(buildingID));
        }

        /// <summary>End the consequence calc: GetTaxRate is back to the real player rate.</summary>
        public static void EndConsequence()
        {
            ActiveRateDelta = 0;
        }

        // fareBurden / taxBase, cached per building per period.
        private static float RateDelta(ushort buildingID)
        {
            EnsurePeriod();
            long burden;
            if (!_burden.TryGetValue(buildingID, out burden) || burden == 0)
                return 0f;
            float cached;
            if (_rateDeltaCache.TryGetValue(buildingID, out cached))
                return cached;
            int income = GetBuildingIncome(buildingID);
            float delta = income > 0 ? (float) burden / income : 0f;
            _rateDeltaCache[buildingID] = delta;
            if (_rateLogBudget > 0)
            {
                _rateLogBudget--;
                int realRate = ResidentialRate(buildingID);
                Log.Info("TicketHappiness: building " + buildingID + " burden=" + burden + " income=" + income +
                         " realRate=" + realRate + " rateDelta=" + delta.ToString("F3") +
                         " -> internalRate=" + (realRate + Mathf.RoundToInt(delta)));
            }
            return delta;
        }

        private static int ResidentialRate(ushort buildingID)
        {
            ref Building b = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            if (b.Info == null || b.Info.m_class == null)
                return 9;
            ItemClass c = b.Info.m_class;
            try { return Singleton<EconomyManager>.instance.GetTaxRate(c.m_service, c.m_subService, c.m_level); }
            catch { return 9; }
        }

        // Building's taxable income = residents' accumulated income, via the protected
        // CommonBuildingAI.GetHomeBehaviour (cached reflection, once per building per period).
        private static int GetBuildingIncome(ushort buildingID)
        {
            try
            {
                ref Building b = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
                CommonBuildingAI ai = b.Info != null ? b.Info.m_buildingAI as CommonBuildingAI : null;
                if (ai == null)
                    return 0;
                if (_getHomeBehaviour == null)
                    _getHomeBehaviour = typeof(CommonBuildingAI).GetMethod("GetHomeBehaviour",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                if (_getHomeBehaviour == null)
                    return 0;
                Citizen.BehaviourData behaviour = new Citizen.BehaviourData();
                object[] args = { buildingID, b, behaviour, 0, 0, 0, 0, 0 };
                _getHomeBehaviour.Invoke(ai, args);
                return ((Citizen.BehaviourData) args[2]).m_incomeAccumulation;
            }
            catch
            {
                return 0;
            }
        }
    }
}
