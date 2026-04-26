# Changelog

## [1.0.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v0.1.0...v1.0.0) (2026-04-26)


### ⚠ BREAKING CHANGES

* remove vehicle assignment feature
* remove unbunching feature (CanLeavePatch and CanLeaveStopPatch)
* remove GetLineVehiclePatch
* remove StartTransferPatch and CheckTransportLineVehiclesPatch
* remove GetVehicleInfoPatch (PublicTransportLineVehicleSelector)
* remove RefreshVehicleButtonsPatch (vehicle tooltip on line panel)

### Features

* add debug logging with per-stop throttle ([2c54862](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/2c548626831384d867f9360f7ad738f48b734590))
* add detailed workshop description for IPT Essentials ([3315504](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/3315504b84050e87e1a55fc911da83f96a051b90))
* **data:** bump to v005 — remove Depot/Prefabs/Queue/Unbunching, backward-compat reader for v001-v004 ([f16d55f](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/f16d55f4531d5e6234858e2218087fca7e8ba077))
* **patch:** replace RedirectionFramework reverse detour with Harmony ReversePatch for TransportLine.GetActiveVehicle ([e212ebc](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/e212ebc032196c825f967486fddb3c0bbecd58a0))
* remove GetLineVehiclePatch ([31c1694](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/31c1694bbe33d24621adea36740627ba028a23bb))
* remove GetVehicleInfoPatch (PublicTransportLineVehicleSelector) ([b4616b3](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/b4616b348ef620c085e5380003e5b2fd931a1d4a))
* remove RefreshVehicleButtonsPatch (vehicle tooltip on line panel) ([e512e84](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/e512e845acae4344b8c2823725ab5cfc0945dec4))
* remove StartTransferPatch and CheckTransportLineVehiclesPatch ([3b37e1c](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/3b37e1c61bdffa4bd34bcb954850a0e0a6ee7e15))
* remove unbunching feature (CanLeavePatch and CanLeaveStopPatch) ([50cf1d1](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/50cf1d1711d0f93233bb03a52f1aad246a65de9d))
* remove vehicle assignment feature ([cee6403](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/cee640301425438dc80f91a271d1cff00c0cfe05))
* **ui:** remove depot vehicle list — drop VehicleListBox/Row files, taxi+cable-car section from city service panel ([fde440e](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/fde440e5974370ecd789b47aefdefb41646fd7c5))
* **ui:** slim PanelExtenderLine — remove depot, unbunching, vehicle queues, vehicle type selection ([2f2aa34](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/2f2aa34c461bcb1290c1a2a0629cd0f110629753))


### Bug Fixes

* add numeric suffices to vanilla vehicles ([e398682](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/e398682834004a772e3c4a6c63d043aff9dd1e02))
* deploy Locale folder and fix line panel layout ([b94513c](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/b94513cf2e2b9354722568d2224cea02eede96ef))
* guard UpdateStopButtonsPatch against missing m_stopButtons field ([555805b](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/555805b41321f1cd9af20b282033f6f260fdb7a3))
* open vehicle info panel when vehicle button is clicked. Prevent opening by pressing Alt. ([298fba3](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/298fba3313ae1b6588458865b8c9bc154e75edc4))
* **patch:** replace VehiclePrefabs maintenance with native VehicleInfo.m_maintenanceCost, remove vehicle queue spawning ([9e3d938](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/9e3d9380f8915947485807ed2e0449ff33d751f7))
* polish line info panel UI layout and sizing ([0337208](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/03372081a4ff29f756e72abfcf8bce66b98eb053))
* remove HideVehicleEditor refs and guard progress reflection fields ([b465a84](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/b465a8435973fe774fe3142dfd3fba9f32f2df37))
* remove planes from blimp & helicopter line selection ([f9053a0](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/f9053a0bb17605eb4f38dce7107431aceeda1512))
* replace broken Harmony reverse patch stub with direct reflection ([6d9c7a4](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/6d9c7a41e84bb3f08e643cf7a703652d3849f94a))
* resolve all remaining compilation errors after feature removal ([dd1c853](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/dd1c853f3d58bd3ddddf84ad04ae56ad433bde46))
* **ui:** replace VehiclePrefabs/PrefabData.MaintenanceCost with native VehicleInfo.m_maintenanceCost ([61cd6bb](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/61cd6bbd7633c52354dd6aee7023db25bab7acaf))
* update release-please action to use GITHUB_TOKEN instead of RELEASE_PLEASE_TOKEN ([bb5ae70](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/bb5ae709e72d2fa3730283fddd95773b78a394dd))
* update release-please action to use RELEASE_PLEASE_TOKEN for triggering workflows ([822a453](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/822a4537a69a9e6edc4db609216fe77114829b75))
