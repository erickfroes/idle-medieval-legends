using IdleMedievalLegends.Editor.Inventory;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class InventorySceneValidationTests
    {
        [Test]
        public void InventoryScene_GeneratedComposition_IsValid()
        {
            Assert.That(InventorySceneTools.Validate(), Is.Empty);
        }
    }
}
