using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace GameHook.IntegrationTests
{
    [TestClass]
    public class XmlSpecificTests : BaseTest
    {
        [Ignore]
        [TestMethod]
        public async Task Property_OK_BinaryCodedDecimal()
        {
            await Task.CompletedTask;
        }

        [Ignore]
        [TestMethod]
        public async Task Property_OK_BitFieldProperty()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task Property_OK_BitProperty()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var value_1 = mapper.Properties.Single(x => x.Path == "player.badges.badge2");
            var value_2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.badges.badge2");
            var value_3 = await GameHookClient.GetPropertyAsync("player.badges.badge2");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xD857,
                Bytes = new int[] { 0xFF },
                Position = 1,
                Path = "player.badges.badge2",
                Length = 1,
                Type = "bit",
                Frozen = false,
                Description = "hiveBadge",
                Value = true
            }, value_1);

            GameHookAssert.ArePropertiesEqual(value_1, value_2);
            GameHookAssert.ArePropertiesEqual(value_1, value_3);
        }

        [Ignore]
        [TestMethod]
        public async Task Property_OK_BooleanProperty()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task Property_OK_Integer()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var value_1 = mapper.Properties.Single(x => x.Path == "player.teamCount");
            var value_2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.teamCount");
            var value_3 = await GameHookClient.GetPropertyAsync("player.teamCount");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xDCD7,
                Bytes = new int[] { 0x01 },
                Position = null,
                Path = "player.teamCount",
                Length = 1,
                Type = "int",
                Frozen = false,
                Value = (long)1
            }, value_1);

            GameHookAssert.ArePropertiesEqual(value_1, value_2);
            GameHookAssert.ArePropertiesEqual(value_1, value_3);
        }

        [TestMethod]
        public async Task Property_OK_Integer_Length3()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var value_1 = mapper.Properties.Single(x => x.Path == "player.money");
            var value_2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.money");
            var value_3 = await GameHookClient.GetPropertyAsync("player.money");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xD84E,
                Bytes = new int[] { 0x07, 0xB0, 0x93 },
                Position = null,
                Path = "player.money",
                Length = 3,
                Type = "int",
                Frozen = false,
                Value = (long)503955
            }, value_1);

            GameHookAssert.ArePropertiesEqual(value_1, value_2);
            GameHookAssert.ArePropertiesEqual(value_1, value_3);
        }

        [TestMethod]
        public async Task Property_OK_Integer_Postprocessor()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var value_1 = mapper.Properties.Single(x => x.Path == "player.team.0.move1pp");
            var value_2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.team.0.move1pp");
            var value_3 = await GameHookClient.GetPropertyAsync("player.team.0.move1pp");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xDCF6,
                Bytes = new int[] { 0xD8 },
                Position = null,
                Path = "player.team.0.move1pp",
                Length = 1,
                Type = "int",
                Frozen = false,
                Value = (long)24
            }, value_1);

            GameHookAssert.ArePropertiesEqual(value_1, value_2);
            GameHookAssert.ArePropertiesEqual(value_1, value_3);

            mapper.AssertAreEqual("player.team.0.move1pp", 0xDCF6, new int[] { 0xD8 }, 24);
            mapper.AssertAreEqual("player.team.0.move1ppUp", 0xDCF6, new int[] { 0xD8 }, 3);
        }

        [TestMethod]
        public async Task Property_OK_Reference()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var value_1 = mapper.Properties.Single(x => x.Path == "player.gender");
            var value_2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.gender");
            var value_3 = await GameHookClient.GetPropertyAsync("player.gender");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xD472,
                Bytes = new int[] { 0x01 },
                Position = null,
                Path = "player.gender",
                Length = 1,
                Type = "int",
                Frozen = false,
                Reference = "gender",
                Value = "Female"
            }, value_1);

            GameHookAssert.ArePropertiesEqual(value_1, value_2);
            GameHookAssert.ArePropertiesEqual(value_1, value_3);
        }

        [TestMethod]
        public async Task Property_OK_String()
        {
            await Load_GBC_PokemonCrystal("yml");

            var mapper = await GameHookClient.GetMapperAsync();
            var playerName = mapper.Properties.Single(x => x.Path == "player.name");
            var playerName2 = (await GameHookClient.GetPropertiesAsync()).Single(x => x.Path == "player.name");
            var playerName3 = await GameHookClient.GetPropertyAsync("player.name");

            GameHookAssert.ArePropertiesEqual(new OpenAPI.GameHook.PropertyModel
            {
                Address = 0xD47D,
                Bytes = new int[] { 0x92, 0xA7, 0xA4, 0xAB, 0xAB, 0xB9, 0x50, 0x50, 0x00, 0x00, 0x00 },
                Position = null,
                Path = "player.name",
                Length = 11,
                Type = "string",
                Reference = "defaultCharacterMap",
                Frozen = false,
                Value = "Shellz"
            }, playerName);

            GameHookAssert.ArePropertiesEqual(playerName, playerName2);
            GameHookAssert.ArePropertiesEqual(playerName, playerName3);
        }

        [Ignore]
        [TestMethod]
        public async Task Property_OK_UnsignedInteger()
        {
            await Task.CompletedTask;
        }
    }
}