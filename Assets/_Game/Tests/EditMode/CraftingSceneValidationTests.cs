using IdleMedievalLegends.Editor.Crafting;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class CraftingSceneValidationTests
    {
        [Test]
        public void CraftingScene_CompositionAndBuildSettings_AreValid()
        {
            Assert.That(CraftingSceneTools.Validate(), Is.Empty);
        }
    }
}
