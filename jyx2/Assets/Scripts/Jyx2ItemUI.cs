/*
 * 金庸群侠传3D重制版
 * https://github.com/jynew/jynew
 *
 * 这是本开源项目文件头，所有代码均使用MIT协议。
 * 但游戏内资源和第三方插件、dll等请仔细阅读LICENSE相关授权协议文档。
 *
 * 金庸老先生千古！
 */
using System.Collections;
using System.Collections.Generic;
using HanSquirrel.ResourceManager;
using HSFrameWork.ConfigTable;
using Jyx2;
using UnityEngine;
using UnityEngine.UI;

public class Jyx2ItemUI : MonoBehaviour
{
    public Image m_Image;
    public Text m_NameText;
    public Text m_CountText;

    public static Jyx2ItemUI Create(int id,int count)
    {
        var prefab = Jyx2ResourceHelper.GetCachedPrefab("Assets/Prefabs/Jyx2ItemUI.prefab");

        
        var obj = Instantiate(prefab); //TODO对象池
        var itemUI = obj.GetComponent<Jyx2ItemUI>();
        itemUI.Show(id, count);
        return itemUI;
    }

    private int _id;

    public Jyx2Item GetItem()
    {
        return ConfigTable.Get<Jyx2Item>(_id);
    }

    public void Show(int id,int count)
    {
        _id = id;
        var item = GetItem();//0-阴性内力，1-阳性内力，2-中性内力
		var color=item.ItemType==2? item.NeedMPType==2?ColorStringDefine.Default:item.NeedMPType==1?ColorStringDefine.Mp_type1:ColorStringDefine.Mp_type0:ColorStringDefine.Default;
        m_NameText.text = $"<color={color}>{item.Name}</color>";
        m_CountText.text = (count > 1 ? count.ToString() : "");

        Jyx2ResourceHelper.GetItemSprite(id, m_Image);
    }

    public void Select(bool active) 
    {
        Transform select = transform.Find("Select");
        select.gameObject.SetActive(active);
    }

}
