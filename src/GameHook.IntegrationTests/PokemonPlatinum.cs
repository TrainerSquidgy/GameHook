using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GameHook.IntegrationTests
{
    [TestClass]
    public class PokemonPlatinum : BaseTest
    {
        [TestMethod]
        public async Task Property_OK_Names()
        {
            await Load_NDS_PokemonPlatinum();

            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("player.name", 36168212, new int[] { 0x2B, 0x01, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, "A");
            mapper.AssertAreEqual("player.team.0.nickname", 72, new int[] { 0x3E, 0x01, 0x39, 0x01, 0x3C, 0x01, 0x3E, 0x01, 0x2F, 0x01, 0x3C, 0x01, 0x3C, 0x01, 0x2B, 0x01, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 }, "TORTERRA");
        }

        [TestMethod]
        public async Task Property_OK_Player()
        {
            await Load_NDS_PokemonPlatinum();
            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("player.badges.badge1", 0x0227E22E, new int[] { 0x03 }, true);
        }

        [TestMethod]
        public async Task Property_OK_PokemonPartyStructure_Index0()
        {
            await Load_NDS_PokemonPlatinum();

            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("meta.global_pointer", 0x02101D2C, new int[] { 0xB8, 0x11, 0x27, 0x02 }, 36114872);

            mapper.AssertAreEqual("player.team.0.species", 0x08, new int[] { 0x85, 0x01 }, "Torterra");
            mapper.AssertAreEqual("player.team.0.nature", 0x00, new int[] { 0xB4, 0x00, 0xE8, 0xB5 }, "Adamant");
            mapper.AssertAreEqual("player.team.0.level", 0x8C, new int[] { 0x25 }, 37);
            mapper.AssertAreEqual("player.team.0.moves.move1.move", 0x8C, new int[] { 0x48 }, "Mega Drain");
            mapper.AssertAreEqual("player.team.0.evs.hp", 0x18, new int[] { 0x0E }, 14);
            mapper.AssertAreEqual("player.team.0.evs.specialDefense", 0x1D, new int[] { 0x06 }, 6);
            mapper.AssertAreEqual("player.team.0.flags.isEgg", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, false);
            mapper.AssertAreEqual("player.team.0.internals.personalityValue", 0x00, new int[] { 0xB4, 0x00, 0xE8, 0xB5 }, 3051880628);
            mapper.AssertAreEqual("player.team.0.internals.checksum", 0x06, new int[] { 0xE1, 0xAC }, 44257);
            mapper.AssertAreEqual("player.team.0.expPoints", 0x10, new int[] { 0x97, 0xB1, 0x00, 0x00 }, 45463);
            mapper.AssertAreEqual("player.team.0.heldItem", 0x0A, new int[] { 0xF9, 0x00 }, "Charcoal");

            // Check IVs which make use of the NCalc BitRange function.
            mapper.AssertAreEqual("player.team.0.ivs.hp", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, 4);
            mapper.AssertAreEqual("player.team.0.ivs.attack", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, 4);
            mapper.AssertAreEqual("player.team.0.ivs.defense", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, 4);
            mapper.AssertAreEqual("player.team.0.ivs.hp", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, 4);
            mapper.AssertAreEqual("player.team.0.flags.isEgg", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, false);
        }

        [TestMethod]
        public async Task Property_OK_BattleOutcome()
        {
            await Load_NDS_PokemonPlatinum();
            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("battle.outcome", 0x022C5B2F, new int[] { 0x45 }, 69);
            mapper.AssertAreEqual("battle.player.playerPokemon1.expPoints", 0x022C57B4, new int[] { 0x9C, 0x57, 0x2C, 0x02 }, 36460444); //Data makes no contextual sense because the player is not in battle
            // mapper.AssertAreEqual("battle.ally.team.0.nature", 0x00, new int[] { 0x00, 0x00, 0x00, 0x00 }, "Hardy"); //Data makes no contextual sense because the player is not in battle
            mapper.AssertAreEqual("battle.opponentA.team.0.pokerus", 0x82, new int[] { 0x6A }, 106); //Data makes no contextual sense because the player is not in battle
            mapper.AssertAreEqual("battle.opponentB.team.0.pokedexNumber", 0x08, new int[] { 0x58, 0xF3 }, 62296); //Data makes no contextual sense because the player is not in battle
        }

        [TestMethod]
        public async Task Property_OK_BagStructure()
        {
            await Load_NDS_PokemonPlatinum();
            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("bag.items.0.item", 0x0227E7DC, new int[] { 0xDF, 0x00 }, "Amulet Coin");
            mapper.AssertAreEqual("bag.medicine.0.item", 0x0227ECF8, new int[] { 0x12, 0x00 }, "Antidote");
            mapper.AssertAreEqual("bag.pokeBalls.0.item", 0x0227EE98, new int[] { 0x04, 0x00 }, "Poké Ball");
            mapper.AssertAreEqual("bag.tmhm.0.item", 0x0227EB38, new int[] { 0x48, 0x01 }, "TM01");
            mapper.AssertAreEqual("bag.berries.0.item", 0x0227ED98, new int[] { 0x9C, 0x00 }, "Persim Berry");
        }

        [TestMethod]
        public async Task Property_OK_PokemonPartyStructure_Index0_IsNotEgg()
        {
            await Load_NDS_PokemonPlatinum();
            var mapper = await GameHookClient.GetMapperAsync();

            // The 4th Pokemon in the party is actually an egg.
            mapper.AssertAreEqual("player.team.0.flags.isEgg", 0x38, new int[] { 0xC4, 0x11, 0xAE, 0x23 }, false);
        }

        [TestMethod]
        public async Task Property_OK_PokemonPartyStructure_Index3_IsEgg()
        {
            await Load_NDS_PokemonPlatinum();
            var mapper = await GameHookClient.GetMapperAsync();

            // The 4th Pokemon in the party is actually an egg.
            mapper.AssertAreEqual("player.team.3.flags.isEgg", 0x38, new int[] { 0x9C, 0xFC, 0xF2, 0x51 }, true);
        }
    }
}
