using UnityEngine;

namespace Core.SO
{
    [CreateAssetMenu(menuName = "Meta game/Daily Quests/Quest Sprites Config")]
    public class QuestSpritesConfig : ScriptableObject
    {
        [field: SerializeField] public Sprite SpinReelsSprite { get; private set; }
        [field: SerializeField] public Sprite WinGamesSprite { get; private set; }
        [field: SerializeField] public Sprite CollectDiamondsSprite { get; private set; }
        [field: SerializeField] public Sprite TriggerTurboModeSprite { get; private set; }
        [field: SerializeField] public Sprite ReachMultiplierSprite { get; private set; }
        [field: SerializeField] public Sprite ClaimFreeEnergySprite { get; private set; }
        [field: SerializeField] public Sprite OpenTheVaultSprite { get; private set; }
        [field: SerializeField] public Sprite Hit21Sprite { get; private set; }
        [field: SerializeField] public Sprite LaunchRocketsSprite { get; private set; }
        [field: SerializeField] public Sprite DropPlinkoBallsSprite { get; private set; }
        [field: SerializeField] public Sprite SpinTheLuckyWheelSprite { get; private set; }
        [field: SerializeField] public Sprite RollDoubleDiceSprite { get; private set; }
        [field: SerializeField] public Sprite EarnRCoinsSprite { get; private set; }
        [field: SerializeField] public Sprite CompleteCombosSprite { get; private set; }
        [field: SerializeField] public Sprite UpgradeLevelSprite { get; private set; }
        [field: SerializeField] public Sprite PlayEveryArcadeSprite { get; private set; }
    }
}