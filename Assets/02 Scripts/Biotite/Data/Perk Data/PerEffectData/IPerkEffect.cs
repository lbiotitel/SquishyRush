


//재능 효과의 인터페이스 입니다. 모든 재능효과는 아래를 구현해야합니다.
public interface IPerkEffect
{
    public void Excute(BattleUnit unit, object source);
    public void Release(BattleUnit unit, object source);
}
