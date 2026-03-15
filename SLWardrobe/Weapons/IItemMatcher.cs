#if EXILED
using Exiled.API.Features;
using Exiled.API.Features.Items;
#else
using LabApi.Features.Wrappers;
#endif

namespace SLWardrobe.Weapons
{
    public interface IItemMatcher
    {
        bool Matches(Item item, Player owner);
        string Description { get; }
    }
}