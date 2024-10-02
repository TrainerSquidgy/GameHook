using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GameHook.IntegrationTests
{
    [TestClass]
    public class PokemonRed : BaseTest
    {
        [TestMethod]
        public async Task Property_OK_PokemonPartyStructure_Index0()
        {
            // TODO: Confirm these are correct.
            await Load_GB_PokemonRed();

            var mapper = await GameHookClient.GetMapperAsync();

            mapper.AssertAreEqual("player.team.0.species", 0xD16B, new int[] { 0x15 }, "Mew");
            mapper.AssertAreEqual("player.team.0.level", 0xD18C, new int[] { 0x64 }, 100);
            mapper.AssertAreEqual("player.team.0.moves.0.move", 0xD173, new int[] { 0x9D }, "Rock Slide");
        }

    }
}
