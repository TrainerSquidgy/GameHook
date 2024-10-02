using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GameHook.IntegrationTests
{
    [TestClass]
    public class PokemonEmerald : BaseTest
    {
        [Ignore]
        [TestMethod]
        public async Task Property_OK_DMA_A_Item()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task Property_OK_DMA_B_GameTime()
        {
            await Load_GBA_PokemonEmerald();

            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("gameTime.hours", 0x02024ADA, new int[] { 0x09 }, 9);
            mapper.AssertAreEqual("gameTime.minutes", 0x02024ADC, new int[] { 0x16 }, 22);
            mapper.AssertAreEqual("gameTime.seconds", 0x02024ADD, new int[] { 0x1A }, 26);
            mapper.AssertAreEqual("gameTime.frames", 0x02024ADE, new int[] { 0x14 }, 20);
        }

        [TestMethod]
        public async Task Property_OK_PokemonPartyStructure_Index0()
        {
            // TODO: Confirm these are correct.
            await Load_GBA_PokemonEmerald();

            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("player.team.0.species", 0x20, new int[] { 0x42, 0x01 }, "Sableye");
            mapper.AssertAreEqual("player.team.0.level", 0x54, new int[] { 0x50 }, 80);
            mapper.AssertAreEqual("player.team.0.moves.0.move", 0x2C, new int[] { 0xED, 0x00 }, "Hidden Power");
        }

    }
}
