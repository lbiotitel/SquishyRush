using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



//로고 씬의 로직을 담당 합니다.
public class LogoController : MonoBehaviour
{
    [SerializeField] private Image logoImage;
    [SerializeField] private float fadeInDuration = 1f;

    private void Start()
    {
        Color color = logoImage.color;
        color.a = 0f;
        logoImage.color = color;

        logoImage.DOFade(1, fadeInDuration)
            .OnComplete(() => {
                SceneManager.LoadScene(EScene.Title.ToString());
            });
    }

}
