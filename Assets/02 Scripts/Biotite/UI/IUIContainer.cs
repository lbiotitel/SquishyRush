


//ui 컨테이너들이 사용하는 인터페이스 입니다.
public interface IUIContainer
{
    public BaseUI OpenUI<T>(BaseUIData data = null);
    public void CloseUI(BaseUI ui);
}
