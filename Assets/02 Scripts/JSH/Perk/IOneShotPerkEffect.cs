/// <summary>
/// 획득할 때 한 번만 실행되는 재능 효과입니다. (골드, 상점 경험치 등)
/// InstantPerk가 스탯 재빌드나 합성 상속으로 효과를 다시 실행할 때 이 효과는 건너뜁니다.
/// </summary>
public interface IOneShotPerkEffect : IPerkEffect {}
