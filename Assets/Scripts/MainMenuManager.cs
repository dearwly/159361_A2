using UnityEngine;
using UnityEngine.SceneManagement; // 重要：必须添加这一行才能使用场景管理器！

public class MainMenuManager : MonoBehaviour
{
    // 这个函数将由“开始游戏”按钮来调用
    public void StartGame()
    {
        // 将 "Example" 替换成您自己的游戏场景的准确文件名
        SceneManager.LoadScene("Example"); 
    }

    // 您也可以顺便为退出按钮添加一个函数
    public void QuitGame()
    {
        // 这行代码在编辑器中不会退出，但在最终生成出的游戏中会关闭程序
        Debug.Log("请求退出游戏"); // 在编辑器中打印信息，方便我们确认按钮被按下了
        Application.Quit();
    }
}