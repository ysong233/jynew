/*
 * 金庸群侠传3D重制版
 * https://github.com/jynew/jynew
 *
 * 这是本开源项目文件头，所有代码均使用MIT协议。
 * 但游戏内资源和第三方插件、dll等请仔细阅读LICENSE相关授权协议文档。
 *
 * 金庸老先生千古！
 */
using HanSquirrel.ResourceManager;
using Jyx2;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RoleUIItem : MonoBehaviour
{
    public static RoleUIItem Create()
    {
        var obj = Jyx2ResourceHelper.CreatePrefabInstance(string.Format(GameConst.UI_PREFAB_PATH, "RoleItem"));
        var roleItem = obj.GetComponent<RoleUIItem>();
        roleItem.InitTrans();
        return roleItem;
    }

    Transform m_select;
    Image m_roleHead;
    Text m_roleName;
    Text m_roleInfo;
    RoleInstance m_role;
    List<int> m_showPropertyIds = new List<int>(){ 14, 13, 15 };//要显示的属性

    void InitTrans() 
    {
        m_select = transform.Find("Select");
        m_roleHead = transform.Find("RoleHead").GetComponent<Image>();
        m_roleName = transform.Find("Name").GetComponent<Text>();
        m_roleInfo = transform.Find("Info").GetComponent<Text>();
    }

    public void ShowRole(RoleInstance role, List<int> pros = null) 
    {
        m_role = role;
        if (pros != null)
            m_showPropertyIds = pros;

        string nameText = role.Name + " Lv." + role.Level;
        m_roleName.text = nameText;

        ShowProperty();

        Jyx2ResourceHelper.GetRoleHeadSprite(role, m_roleHead);
    }

    void ShowProperty() 
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < m_showPropertyIds.Count; i++)
        {
            string proId = m_showPropertyIds[i].ToString();
            if (!GameConst.ProItemDic.ContainsKey(proId))
                continue;
            var proItem = GameConst.ProItemDic[proId];
            if (proItem.PropertyName == "Hp")
            {
				var color1 = m_role.GetHPColor1();
				var color2 = m_role.GetHPColor2();
                sb.Append($"{proItem.Name}:<color={color1}>{m_role.Hp}</color>/<color={color2}>{m_role.MaxHp}</color>\n");
            }
            else if (proItem.PropertyName == "Tili")
            {
                sb.Append($"{proItem.Name}:{m_role.Tili}/{GameConst.MaxTili}\n");
            }
            else if (proItem.PropertyName == "Mp")
            {
				var color = m_role.GetMPColor();
		        sb.Append($"{proItem.Name}:<color={color}>{m_role.Mp}/{m_role.MaxMp}</color>\n");
            }
            else 
            {
                int value = (int)m_role.GetType().GetProperty(proItem.PropertyName).GetValue(m_role, null);
                sb.Append($"{proItem.Name}:{value}\n");
            }
        }
        m_roleInfo.text = sb.ToString();
    }

    public void SetSelect(bool active) 
    {
        m_select.gameObject.SetActive(active);
    }

    public RoleInstance GetShowRole() 
    {
        return m_role;
    }
}
