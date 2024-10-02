using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GameHook.IntegrationTests
{
    [TestClass]
    public class MapperTests : BaseTest
    {
        [TestMethod]

        #region Gameboy
        // TODO: Add Red + Blue SRM combinations, should be expanded to 8.
        [DataRow("gb_pokemon_red_blue_xml", "gb_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_red_blue_yml", "gb_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_red_blue_xml", "gbc_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_red_blue_yml", "gbc_pokemon_red_blue_0")]

        [DataRow("gb_pokemon_yellow_xml", "gb_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_yellow_yml", "gb_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_yellow_xml", "gbc_pokemon_red_blue_0")]
        [DataRow("gb_pokemon_yellow_yml", "gbc_pokemon_red_blue_0")]
        #endregion

        #region Gameboy Color
        [DataRow("gbc_pokemon_crystal_xml", "gbc_pokemon_crystal_0")]
        [DataRow("gbc_pokemon_crystal_yml", "gbc_pokemon_crystal_0")]

        // TODO: Add Gold + Silver SRM combinations, should be expanded to 4.
        [DataRow("gbc_pokemon_gold_silver_xml", "gbc_pokemon_crystal_0")]
        [DataRow("gbc_pokemon_gold_silver_yml", "gbc_pokemon_crystal_0")]

        [DataRow("gbc_zelda_links_awakening_dx_yml", "gbc_pokemon_crystal_0")]
        #endregion

        #region Gameboy Advance
        [DataRow("gba_metroid_fusion_yml", "gba_pokemon_emerald_0")]

        [DataRow("gba_pokemon_emerald_xml", "gba_pokemon_emerald_0")]
        [DataRow("gba_pokemon_emerald_yml", "gba_pokemon_emerald_0")]

        [DataRow("gba_pokemon_firered_xml", "gba_pokemon_emerald_0")]
        [DataRow("gba_pokemon_firered_yml", "gba_pokemon_emerald_0")]

        [DataRow("gba_pokemon_rubysapphire_xml", "gba_pokemon_emerald_0")]
        [DataRow("gba_pokemon_sapphire_yml", "gba_pokemon_emerald_0")]
        #endregion

        #region Nintendo 64
        [DataRow("n64_mario_kart_64_xml", "gba_pokemon_emerald_0")]
        #endregion

        #region Nintendo DS
        [DataRow("nds_pokemon_platinum_xml", "nds_pokemon_platinum_0")]
        #endregion

        #region NES
        [DataRow("nes_dragon_quest_1_yml", "gba_pokemon_emerald_0")]
        #endregion

        #region Playstation
        [DataRow("psx_resident_evil_3_nemesis_xml", "gba_pokemon_emerald_0")]
        #endregion

        #region Super Nintendo
        [DataRow("snes_bs_zelda_xml", "gba_pokemon_emerald_0")]
        #endregion

        public async Task DoesMapperLoad(string mapperName, string srmName)
        {
            mapperName = $"official_{mapperName}";
            srmName = $"{srmName}.json";

            Logger.LogInformation(string.Empty);
            Logger.LogInformation("=================================");
            Logger.LogInformation($"Mapper:\t{mapperName}");
            Logger.LogInformation($"SRM:\t\t{srmName}");
            Logger.LogInformation("=================================");
            Logger.LogInformation(string.Empty);

            await LoadSrm(srmName);
            await LoadMapper(mapperName);
        }
    }
}
