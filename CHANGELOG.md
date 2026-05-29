# Changelog

## [1.7.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.6.0...v1.7.0) (2026-05-29)


### Features

* add workshop integration to deployment and mounting scripts ([9cda5a8](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/9cda5a8ff5fbac36ca2deb19f57bd1e21e28dc4b))

## [1.6.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.5.1...v1.6.0) (2026-05-05)


### Features

* restore unbunching feature ([5b4885e](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/5b4885e501e3ff2e5904573c5ee8e1e6dfd490ee))

## [1.5.1](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.5.0...v1.5.1) (2026-05-03)


### Bug Fixes

* update workflow to install Mono and MSBuild together ([09f843c](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/09f843ce8b1e7912a1d9dc239f48090199664c04))

## [1.5.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.4.0...v1.5.0) (2026-05-03)


### Features

* build mod in CI using committed game reference DLLs ([352d9d1](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/352d9d182fbdb258d1b0285db619ab53de3137a8))

## [1.4.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.3.1...v1.4.0) (2026-05-03)


### Features

* inject version from release-please manifest into AssemblyInfo ([b59b1aa](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/b59b1aa61516faa431334dca8e7efd17dcd5ff36))

## [1.3.1](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.3.0...v1.3.1) (2026-05-03)


### Bug Fixes

* detect extended vehicle limits by actual buffer size instead of mod ID ([7b9481d](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/7b9481dd5cbfae900bf9c9446e41b0f542156880))
* prevent IndexOutOfRange when non-transit vehicles despawn ([c0526c9](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/c0526c9ef3529da40c82802e823f99a1d1bdc5f0))
* refactor localization strings for unbunching feature across multiple languages ([4cb1743](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/4cb17430cf502a09f48fe351f3bf80b5011a689a))

## [1.3.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.2.0...v1.3.0) (2026-04-30)


### Features

* add cost per line statistics to line info panel and update localization files ([941cda6](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/941cda6e91cc1a7bab415a89309da7737b166f7b))
* add tooltips for adding vehicles in multiple languages ([2ecccdb](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/2ecccdb062c0768f506314d0f69114af95de7c2b))


### Bug Fixes

* prevent multi-mesh rendering issues in vehicle selection and remove unused rendering methods ([7024637](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/702463788c53c184494169bdf046ca7684df95ee))

## [1.2.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.1.0...v1.2.0) (2026-04-28)


### Features

* add LineVehiclePanel and LineVehicleRow components for improved vehicle display ([ad44a16](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/ad44a16db34acdd5fe5e2b820f1b8e2f460e1152))
* implement vehicle join/leave tracking and enhance UI components ([0213392](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/02133923caa651d78f877a537c134b468e64ee11))

## [1.1.0](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.0.4...v1.1.0) (2026-04-28)


### Features

* add vehicle type selection feature and improve UI components ([97233d6](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/97233d6f721c4d1dbfab85d5b726a711432349bf))

## [1.0.4](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.0.3...v1.0.4) (2026-04-27)


### Bug Fixes

* guard vehicle data deserialization against out-of-range indices ([143bd9f](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/143bd9f4675066c5f5a159ba01d99b870a5887b2))

## [1.0.3](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.0.2...v1.0.3) (2026-04-27)


### Bug Fixes

* update link for Advanced Stop Selection mod in description ([bc03185](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/bc031853792e4ddc50137d8a8484bdfb6a14f53d))

## [1.0.2](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.0.1...v1.0.2) (2026-04-27)


### Bug Fixes

* set canFocus=false on stop info panel to unblock BOB's UUI hotkey ([79d4b97](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/79d4b97c764da8e36903c16122f30e51e7725edb))

## [1.0.1](https://github.com/roberto-naharro/ImprovedPublicTransport/compare/v1.0.0...v1.0.1) (2026-04-27)


### Bug Fixes

* remove unused GameDefault and GetDepotLevelsPatch classes ([4b074ee](https://github.com/roberto-naharro/ImprovedPublicTransport/commit/4b074eed54449a8affc3eeb4b12abd5e21861d7f))

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
