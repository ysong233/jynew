/*
 * 金庸群侠传3D重制版
 * https://github.com/jynew/jynew
 *
 * 这是本开源项目文件头，所有代码均使用MIT协议。
 * 但游戏内资源和第三方插件、dll等请仔细阅读LICENSE相关授权协议文档。
 *
 * 金庸老先生千古！
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cinemachine;
using DG.Tweening;
using Hanjiasongshu;
using HanSquirrel.ResourceManager;
using Jyx2;
using HSFrameWork.ConfigTable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using XLua;
using UnityEngine.Playables;
using Sirenix.Utilities;
using UnityEngine.Timeline;

namespace Jyx2
{
    public class JYX2EventContext
    {
        public int currentItemId;
        
        static public JYX2EventContext current = null;
    }

    /// <summary>
    /// lua的桥接函数
    /// 
    /// 注意：桥接函数都不是运行在Unity主线程中
    /// </summary>
    [LuaCallCSharp]
    static public class Jyx2LuaBridge
    {
        static StoryEngine storyEngine { get { return StoryEngine.Instance; } }

        static Semaphore sema = new Semaphore(0, 1);

        static GameRuntimeData runtime { get { return GameRuntimeData.Instance; } }

        static public void Talk(int roleId, string content, string talkName, int type)
        {
            RunInMainThread(() =>
            {
                storyEngine.BlockPlayerControl = true;
                //storyEngine.dialogPanel.Show(
                Jyx2_UIManager.Instance.ShowUI("ChatUIPanel", ChatType.RoleId,roleId, content, type,new Action(()=> 
                {
                    storyEngine.BlockPlayerControl = false;
                    Next();
                }));
            });
            
            Wait();
        }

        /// <summary>
        /// 添加（减少）物品，不显示提示
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count"></param>
        static public void AddItemWithoutHint(int itemId, int count)
        {
            RunInMainThread(() =>
            {
                var item = ConfigTable.Get<Jyx2Item>(itemId);
                if (item == null)
                {
                    Debug.LogError("调用了未定义的物品:" + itemId);
                    return;
                }

                runtime.AddItem(itemId, count);
            });
        }

        /// <summary>
        /// 修改事件
        /// </summary>
        /// <param name="scene">场景，-2为当前场景</param>
        /// <param name="eventId">事件ID，-2位保留</param>
        /// <param name="canPass">是否能够经过，-2位保留</param>
        /// <param name="changeToEventId">修改为的编号，-2为保留</param>
        /// <param name="interactiveEventId">交互事件ID</param>
        /// <param name="useItemEventId">使用道具事件ID</param>
        /// <param name="enterEventId">进入事件ID</param>
        /// <param name="p7">开始贴图</param>
        /// <param name="p8">结束贴图</param>
        /// <param name="p9">起始贴图</param>
        /// <param name="p10">动画延迟</param>
        /// <param name="p11">X坐标</param>
        /// <param name="p12">Y坐标</param>
        static public void ModifyEvent(
            int scene,
            int eventId,
            int canPass,
            int changeToEventId,
            int interactiveEventId,
            int useItemEventId,
            int enterEventId,
            int p7,int p8,int p9,int p10,int p11,int p12)
        {
            RunInMainThread(() => {

				bool isCurrentScene=false;
                //场景ID
                if(scene == -2) //当前场景
                {
                    scene = int.Parse(LevelMaster.Instance.GetCurrentGameMap().Jyx2MapId);
					isCurrentScene=true;
                }

				var evt = GameEvent.GetCurrentGameEvent();
                //事件ID
                if(eventId == -2)
                {
                    if (evt == null)
                    {
                        Debug.LogError("内部错误：当前的eventId为空，但是指定修改当前event");
                        Next();
                        return;
                    }
                    eventId = int.Parse(evt.name); //当前事件
                }else{
					evt=GameEventManager.GetGameEventByID(eventId.ToString());
				}
				
				if(isCurrentScene && evt!=null)//非当前场景事件如何获取
				{
					if(interactiveEventId==-2){
						interactiveEventId=evt.m_InteractiveEventId;
					}
					
					if(useItemEventId==-2){
						useItemEventId=evt.m_UseItemEventId;
					}
					
					if(enterEventId==-2){
						enterEventId=evt.m_EnterEventId;
					}
				}

                //更新全局记录
                runtime.ModifyEvent(scene, eventId, interactiveEventId, useItemEventId, enterEventId);
				
				if(p7!=-2){
					runtime.SetMapPic(scene,eventId,p7);
				}

                //刷新当前场景中的事件
                LevelMaster.Instance.RefreshGameEvents();

                //下一条指令
                Next();
            });

            Wait();
        }


        //询问是否战斗
        static public bool AskBattle()
        {
            return ShowYesOrNoSelectPanel("是否与之战斗?");
        }

        static private bool _battleResult = false;

        static public bool isQuickBattle = false;
        //开始一场战斗
        static public bool TryBattle(int battleId)
        {
            if(isQuickBattle)
            {
                return ShowYesOrNoSelectPanel("是否战斗胜利?");
            }

            bool isWin = false;
            RunInMainThread(() => {
                var pos = LevelMaster.Instance.GetPlayerPosition();
                string posStr = UnityTools.Vector3ToString(pos);
                string posOri = UnityTools.QuaternionToString(LevelMaster.Instance.GetPlayerOrientation());
                string currentScene = SceneManager.GetActiveScene().name;
                LevelLoader.LoadBattle(battleId, (ret) => {
                    LevelLoader.LoadGameMap(ConfigTable.Get<GameMap>(currentScene), new LevelMaster.LevelLoadPara() {
                        loadType = LevelMaster.LevelLoadPara.LevelLoadType.StartAtPos,
                        CurrentPos = posStr,
						CurrentOri = posOri,
                    }, "", ()=> {
                        isWin = (ret == BattleResult.Win);
                        Next();
                    });
                });
            });
            Wait();
            return isWin;
        }

        //替换当前的出门音乐
        static public void ChangeMMapMusic(int musicId)
        {
            RunInMainThread(() => {
                LevelMaster.Instance.GetCurrentGameMap().ForceSetLeaveMusicId = musicId;
            });
        }

        static public bool AskJoin()
        {
            return ShowYesOrNoSelectPanel("是否要求加入?");
        }

        //角色加入，同时获得对方身上的物品
        static public void Join(int roleId)
        {
            RunInMainThread(() => {
                RoleInstance role = runtime.GetRole(roleId);
                if (role == null)
                {
                    Debug.LogError("加入了未定义角色，id=" + roleId);
                    return;
                }
                runtime.JoinRoleToTeam(roleId);
                storyEngine.DisplayPopInfo(role.Name + "加入队伍！");

                //同时获得对方身上的物品
                foreach (var item in role.Items)
                {
                    if (item.Count == 0) item.Count = 1;
                    AddItem(item.Id, item.Count);
                }
                role.Items.Clear();
                Next();
            });
            Wait();
        }
        
        static public void Dead()
        {
            RunInMainThread(() => {
                
                //TODO..
                MessageBox.Create("GAME OVER", () =>
                {
                    LevelMaster.Instance.QuitToMainMenu();
                });
            });
        }

        static public bool HaveItem(int itemId)
        {
            return runtime.HaveItemBool(itemId);
        }

        //当前正在使用的物品ID
        static public bool UseItem(int itemId)
        {
            if (JYX2EventContext.current == null)
                return false;

            return itemId == JYX2EventContext.current.currentItemId;
        }

        //离队
        static public void Leave(int roleId)
        {
            RunInMainThread(() => {
                runtime.LeaveTeam(roleId);
                Next();
            });
            Wait();
        }

        static public void ZeroAllMP()
        {
            RunInMainThread(() => {
                foreach (var r in runtime.Team)
                {
                    r.Mp = 0;
                }
                Next();
            });
            Wait();
        }

        //设置用毒
        static public void SetOneUsePoi(int roleId, int v)
        {
            RunInMainThread(() =>
            {
                var role = runtime.GetRole(roleId);
                if (role != null)
                {
                    role.UsePoison = v;
                }
                else
                {
                    Debug.LogError("设置用毒，但角色不在队伍，roleId =" + roleId);
                }
                Next();
            });
            Wait();
        }
        
        static public void ScenceFromTo(int x,int y,int x2,int y2)
        {
            //重制版不需要再实现，使用  jyx2_CameraFollow、jyx2_CameraFollowPlayer

        }
        
		//修改这个接口逻辑为在当前trigger对应事件序号基础上加上v1,v2,v3 (只对大于0的进行相加，-2保留原事件序号，-1为直接设置)
		// modified by eaphone at 2021/6/12
        static public void Add3EventNum(int scene, int eventId,int v1,int v2,int v3)
        {
			RunInMainThread(() =>
            {
				bool isCurrentScene=false;
                //场景ID
                if (scene == -2) //当前场景
                {
                    scene = int.Parse(LevelMaster.Instance.GetCurrentGameMap().Jyx2MapId);
					isCurrentScene=true;
                }

				var evt=GameEvent.GetCurrentGameEvent();
                //事件ID
                if (eventId == -2)
                {
                    if (evt == null)
                    {
                        Debug.LogError("内部错误：当前的eventId为空，但是指定修改当前event");
                        return;
                    }
                    eventId = int.Parse(evt.name); //当前事件
                }else{
					evt=GameEventManager.GetGameEventByID(eventId.ToString());
				}

				if(isCurrentScene && evt!=null)//非当前场景事件如何获取
				{
					if(v1==-2){//值为-2时，取当前值
						v1=evt.m_InteractiveEventId;
					}else if(v1>-1){
						v1+=evt.m_InteractiveEventId;
					}
					if(v2==-2){
						v2=evt.m_UseItemEventId;
					}else if(v2>-1){
						v2+=evt.m_UseItemEventId;
					}
					if(v3==-2){
						v3=evt.m_EnterEventId;
					}else if(v3>-1){
						v3+=evt.m_EnterEventId;
					}
					
					runtime.ModifyEvent(scene, eventId, v1, v2, v3);

					//刷新当前场景中的事件
					LevelMaster.Instance.RefreshGameEvents();
				}else{
					if(v1>0){
						runtime.AddEventCount(scene,eventId,0,v1);
					}
					if(v2>0){
						runtime.AddEventCount(scene,eventId,1,v2);
					}
					if(v3>0){
						runtime.AddEventCount(scene,eventId,2,v3);
					}
				}

                //下一条指令
                Next();
			});
            Wait();
        }
		
		//targetEvent:0-interactiveEvent, 1-useItemEvent, 2-enterEvent
		static public int jyx2_CheckEventCount(int scene, int eventId, int targetEvent)
        {
			int result=0;
			RunInMainThread(() =>
            {
				//场景ID
                if (scene == -2) //当前场景
                {
                    scene = int.Parse(LevelMaster.Instance.GetCurrentGameMap().Jyx2MapId);
                }

                //事件ID
                if (eventId == -2)
                {
					var evt=GameEvent.GetCurrentGameEvent();
                    if (evt == null)
                    {
                        Debug.LogError("内部错误：当前的eventId为空，但是指定修改当前event");
                        return;
                    }
                    eventId = int.Parse(evt.name); //当前事件
                }
				
				result= runtime.GetEventCount(scene,eventId,targetEvent);
                Next();
			});
            Wait();
			return result;
		}

        static public bool InTeam(int roleId)
        {
            return runtime.Team.Exists(r => r.Key == roleId.ToString());
        }

		// modify the logicc, when count>=6, team is full
		// by eaphone at 2021/6/5
        static public bool TeamIsFull()
        {
            return runtime.Team.Count > GameConst.MAX_TEAMCOUNT-1;
        }

        /// <summary>
        /// 修改地图
        /// </summary>
        /// <param name="sceneId">场景ID,-2为当前场景</param>
        /// <param name="layer">层级</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="v">贴图编号（需要除以2）</param>
        static public void SetScenceMap(int sceneId,int layer,int x,int y,int v)
        {
            //这个函数已经不需要实现，具体2D和3D版差异解决的方式可以参考
            //https://github.com/jynew/jynew/wiki/1.5%E6%90%AD%E5%BB%BA%E6%B8%B8%E6%88%8F%E4%B8%96%E7%95%8C%E5%B7%AE%E5%BC%82%E8%A7%A3%E5%86%B3%E5%8A%9E%E6%B3%95
            
        }

        //增加道德
        static public void AddEthics(int add)
        {
            runtime.Player.Pinde = HSFrameWork.Common.Tools.Limit(runtime.Player.Pinde + add, 0, 100);
        }

        static public void ChangeScencePic(int p1,int p2,int p3,int p4)
        {
            //这个函数已经不需要实现，具体2D和3D版差异解决的方式可以参考
            //https://github.com/jynew/jynew/wiki/1.5%E6%90%AD%E5%BB%BA%E6%B8%B8%E6%88%8F%E4%B8%96%E7%95%8C%E5%B7%AE%E5%BC%82%E8%A7%A3%E5%86%B3%E5%8A%9E%E6%B3%95

        }

        //播放动画
        static public void PlayAnimation(int p1,int p2,int p3) 
        {
            //这个函数已经不需要实现，使用jyx2_PlayTimeline来解决
        }

        static public bool JudgeEthics(int roleId,int low,int high)
        {
            return JudgeRoleValue(roleId, (r) => { return r.Pinde >= low && r.Pinde <= high; });
        }

        static public bool JudgeAttack(int roleId,int low,int high)
        {
            bool ret = JudgeRoleValue(roleId, (r) => { return r.Attack >= low && r.Attack <= high; });
            return ret;
        }

        static public void WalkFromTo(int x1,int y1,int x2,int y2)
        {
            //这个函数已经不需要实现，使用jyx2_WalkFromTo来解决
        }


        static public void LearnMagic2(int roleId,int magicId,int noDisplay)
        {
            RunInMainThread(() => {
                var role = runtime.GetRole(roleId);
                role.LearnMagic(magicId);

                if(noDisplay != 0)
                {
                    var skill = ConfigTable.Get<Jyx2Skill>(magicId);
                    storyEngine.DisplayPopInfo(role.Name + "习得武学" + skill.Name);
                }
                Next();
            });
            Wait();
        }

        //增加资质
        static public void AddAptitude(int roleId,int v)
        {
            RunInMainThread(() =>
            {
                var role = runtime.GetRole(roleId);
                role.IQ = HSFrameWork.Common.Tools.Limit(role.IQ + v, 0, GameConst.MAX_ZIZHI);
                storyEngine.DisplayPopInfo(role.Name + "资质增加" + v);
                Next();
            });
            Wait();
        }

        static public void SetOneMagic(int roleId,int magicIndexRole,int magicId, int level)
        {
            RunInMainThread(() =>
            {
                var role = runtime.GetRole(roleId); 

                if (role == null)
                {
                    Debug.LogError("调用不了在队伍的角色,roleId =" + roleId);
                }

                if(magicIndexRole >= role.Wugongs.Count)
                {
                    Debug.LogError("SetOneMagic调用错误，index越界");
                    return;
                }

                role.Wugongs[magicIndexRole].Key = magicId;
                role.Wugongs[magicIndexRole].Level = level;
                Next();
            });
            Wait();
        }

        static public bool JudgeSexual(int sexual)
        {
            return JudgeRoleValue(0, r => r.Sex == sexual);
        }

        //判断队伍中是否有女性
        static public bool JudgeFemaleInTeam()
        {
            foreach(var r in runtime.Team)
            {
                if (r.Sex == 1)
                    return true;
            }
            return false;
        }
        
        static public void Play2Amination(int eventIndex1, int beginPic1, int endPic1, int eventIndex2, int beginPic2, int endPic2)
        {
            //这个函数已经不需要实现，使用jyx2_PlayTimeline来解决
        }

        //增加轻功
        static public void AddSpeed(int roleId, int value)
        {
            RunInMainThread(() => 
            {
                var r = runtime.GetRole(roleId);
                var v0 = r.Qinggong;
                r.Qinggong = HSFrameWork.Common.Tools.Limit(v0 + value, 0, GameConst.MAX_ROLE_ATTRITE);
                storyEngine.DisplayPopInfo(r.Name + "轻功增加" + (r.Qinggong - v0));
            });
        }

        //内力
        static public void AddMaxMp(int roleId, int value)
        {
            RunInMainThread(() =>
            {
                var r = runtime.GetRole(roleId);
                var v0 = r.MaxMp;
                r.MaxMp = HSFrameWork.Common.Tools.Limit(v0 + value, 0, GameConst.MAX_HPMP);
                storyEngine.DisplayPopInfo(r.Name + "内力增加" + (r.MaxMp - v0));
            });
        }

        //攻击力
        static public void AddAttack(int roleId, int value)
        {
            RunInMainThread(() =>
            {
                var r = runtime.GetRole(roleId);
                var v0 = r.Attack;
                r.Attack = HSFrameWork.Common.Tools.Limit(v0 + value, 0, GameConst.MAX_ROLE_ATTRITE);
                storyEngine.DisplayPopInfo(r.Name + "内力增加" + (r.Attack - v0));
            });
        }

        //生命值
        static public void AddHp(int roleId, int value)
        {
            RunInMainThread(() =>
            {
                var r = runtime.GetRole(roleId);
                var v0 = r.MaxHp;
                r.MaxHp = HSFrameWork.Common.Tools.Limit(v0 + value, 0, GameConst.MAX_HPMP);
                storyEngine.DisplayPopInfo(r.Name + "生命增加" + (r.MaxHp - v0));
            });
        }

        //设置角色内力属性
        static public void SetPersonMPPro(int roleId, int value)
        {
            var r = runtime.GetRole(roleId);
            r.MpType = value;
        }

        static public void instruct_50(int p1,int p2,int p3,int p4,int p5,int p6,int p7)
        {

        }

        static public void ShowEthics()
        {
            RunInMainThread(() => {
                MessageBox.Create("你的道德指数为" + runtime.Player.Pinde, Next);
            });
            Wait();
        }

        static public void ShowRepute()
        {
            RunInMainThread(() =>
            {
                MessageBox.Create("你的声望指数为" + runtime.Player.Shengwang, Next);
            });
            Wait();
        }

        static public bool JudgeEventNum(int eventIndex, int value)
        {
            bool result = false;
            RunInMainThread(() => {
                var evt = GameEvent.GetCurrentSceneEvent(eventIndex.ToString());
                if(evt != null)
                {
                    result = (evt.m_InteractiveEventId == value);
                }
                Next();
            });
            Wait();
            return result;
        }

        //打开所有场景
        static public void OpenAllScene()
        {
            foreach(var map in ConfigTable.GetAll<Jyx2Map>())
            {
                runtime.SetSceneEntraceCondition(map.Id, 0);
            }
            runtime.SetSceneEntraceCondition("2", 2); //云鹤崖 需要轻功大于75
            runtime.SetSceneEntraceCondition("38", 2); //摩天崖 需要轻功大于75
            runtime.SetSceneEntraceCondition("75", 1); //桃花岛
            runtime.SetSceneEntraceCondition("80", 1); //绝情谷底
        }

        //武林大会
        static public void FightForTop()
        {
			Dictionary<int, string> heads= new Dictionary<int, string>();
			heads.Add(8,"唐文亮来领教阁下的高招．");
			heads.Add(21,"贫尼定闲愿领教阁下高招．");
			heads.Add(23,"贫道天门领教阁下高招．");
			heads.Add(31,"小兄弟，我们再来玩玩．");
			heads.Add(32,"小兄弟，秃笔翁陪你玩玩．");
			heads.Add(43,"白某愿领教阁下高招．");
			heads.Add(7,"何太冲来领教阁下的高招．");
			heads.Add(11,"杨逍技痒，和少侠玩玩．");
			heads.Add(14,"韦一笑技痒，和少侠玩玩．");
			heads.Add(20,"莫某再次领教阁下高招．");
			heads.Add(33,"小兄弟，黑白子向你讨教．");
			heads.Add(34,"小兄弟，黄钟公向你讨教．");
			heads.Add(10,"范某技痒，和少侠玩玩．");
			heads.Add(12,"老朽技痒，和少侠玩玩．");
			heads.Add(19,"岳某不才，向少侠挑战．");
			heads.Add(22,"左冷禅愿领教阁下高招．");
			heads.Add(56,"黄蓉愿领教阁下高招．");
			heads.Add(68,"丘处机领教阁下高招．");
			heads.Add(13,"谢某技痒，和少侠玩玩．");
			heads.Add(55,"郭靖愿领教阁下高招．");
			heads.Add(62,"老夫领教少侠高招！");
			heads.Add(67,"裘千仞来领教阁下的高招．");
			heads.Add(70,"阿弥陀佛，贫道愿向少侠挑战．");
			heads.Add(71,"洪某拜教！");
			heads.Add(26,"任某来领教阁下的高招．");
			heads.Add(57,"少侠的确武功高强，我黄老邪来领教领教．");
			heads.Add(60,"让我老毒物来会会你．");
			heads.Add(64,"哇！你又学了这麽多新奇的功夫.来，来，老顽童陪你玩玩．");
			heads.Add(3,"苗某向少侠讨教．");
			heads.Add(69,"不错不错，七公我来领教领教．");
			var ran=new System.Random();
			var keys=heads.Keys.ToList();
			var values=heads.Values.ToList();
			for(int i=0;i<5;i++)
			{
				var tempList=new List<int>();
				for(int i2=0;i2<3;i2++)
				{
					int j=ran.Next(0,6);
					while(tempList.Contains(j))
					{
						j=ran.Next(0,6);
					}
					tempList.Add(j);
					Talk(keys[i*6+j],values[i*6+j],"",0);
					if (!TryBattle(102 + i*6+j))
					{
						Dead();
						return;
					}
				}
				if(i!=4){
					Talk(70,"少侠已连战三场，可先休息再战．","talkname0", 0);
					Rest();
					DarkScence();
					LightScence();
				}
			}
			
			Talk(0,"接下来换谁？","talkname0", 1);
			Talk(0,"．．．．．．．．","talkname0", 1);
			Talk(0,"没有人了吗？","talkname0", 1);
			Talk(70,"如果还没有人要出来向这位少侠挑战，那麽这武功天下第一之名，武林盟主之位，就由这位少侠夺得．","talkname0", 0);
			Talk(70,"．．．．．．．．．．．．．．．．．．","talkname0", 0);
			Talk(70,"好，恭喜少侠，这武林盟主之位就由少侠获得，而这把”武林神杖”也由你保管．","talkname0", 0);
			Talk(12,"恭喜少侠！","talkname0", 0);
			Talk(64,"小兄弟，恭喜你！","talkname0", 0);
			Talk(19,"好，今年的武林大会到此已圆满结束，希望明年各位武林同道能再到我华山一游．","talkname0", 0);
			DarkScence();
			jyx2_ReplaceSceneObject("","NPC/华山弟子","");
			jyx2_ReplaceSceneObject("","NPC/battleNPC","");
			LightScence();
			Talk(0,"历经千辛万苦，我终于打败群雄，得到这武林盟主之位及神杖．但是”圣堂”在那呢？为什麽没人告诉我，难道大家都不知道．这会儿又有的找了．","talkname0", 1);
			AddItem(143,1);
        }

        //所有人离队
        static public void AllLeave()
        {
            RunInMainThread(() => {
                Debug.Log("call AllLeave()");
                Debug.Log(runtime.Team.Count);
                runtime.Team.ForEach(r => Debug.Log(r.Key));
                runtime.Team.RemoveAll(role => role.Key != "0");
                Next();
            });
            Wait();
        }

		//判断场景贴图。ModifyEvent里如果p7!=-2时，会更新对应{场景}_{事件}的贴图信息，可以用此方法JudegeScenePic检查对应的贴图信息
        static public bool JudgeScenePic(int scene, int eventId, int pic)
        {
            bool result = false;
            RunInMainThread(() => {
				//场景ID
                if(scene == -2) //当前场景
                {
                    scene = int.Parse(LevelMaster.Instance.GetCurrentGameMap().Jyx2MapId);
                }

                //事件ID
                if(eventId == -2)
                {
					var evt = GameEvent.GetCurrentGameEvent();
                    if (evt != null)
                    {
                        eventId = int.Parse(evt.name); //当前事件
					}
                }
				var _target=runtime.GetMapPic(scene, eventId);
				//Debug.LogError(_target);
                result = _target==pic;
                Next();
            });
            Wait();
            return result;
        }

        static public bool Judge14BooksPlaced()
        {
            return jyx2_CheckEventCount(82,999,0)==14;
        }

        static public void EndAmination(int p1, int p2, int p3, int p4, int p5, int p6, int p7)
        {

        }

        static public void SetSexual(int roleId,int sexual)
        {
            var role = runtime.GetRole(roleId);
            if(role != null)
            {
                role.Sex = sexual;
            }
        }

        static public void PlayMusic(int id)
        {
            RunInMainThread(() =>
            {
                AudioManager.PlayMusic(id);
            });
        }

        //标记一个场景是否可以进入
        static public void OpenScene(int sceneId)
        {
            runtime.SetSceneEntraceCondition(sceneId.ToString(), 0);
        }

		// modify by eaphone at 2021/6/5
        static public void SetRoleFace(int dir)
        {
			RunInMainThread(() =>
            {
                var levelMaster = GameObject.FindObjectOfType<LevelMaster>();
				levelMaster.SetRotation(dir);
				Next();
            });
			Wait();
        }

        static public void NPCGetItem(int roleId,int itemId,int count)
        {
            runtime.RoleGetItem(roleId, itemId, count);
        }

        static public void PlayWave(int waveIndex)
        {
            RunInMainThread(()=>
            {
                string path = "Assets/BuildSource/sound/e" +
                    (waveIndex < 10 ? "0" + waveIndex.ToString() : waveIndex.ToString()) + ".wav";

                Jyx2ResourceHelper.LoadAsset<AudioClip>(path, clip =>
                {
                    AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
                });
            });
        }

     
        static public bool AskRest()
        {
            return ShowYesOrNoSelectPanel("是否休息?");
        }

        static public void DarkScence()
        {
            RunInMainThread(() =>
            {
                var blackCover = LevelMaster.Instance.transform.Find("UI/BlackCover");
                if (blackCover == null)
                {
                    Debug.LogError("DarkScence error，找不到LevelMaster/UI/BlackCover");
                    Next();
                    return;
                }

                blackCover.gameObject.SetActive(true);
                var img = blackCover.GetComponent<Image>();
                img.DOFade(1, 1).OnComplete(Next);
            });
            Wait();
        }

        static public void Rest()
        {
            RunInMainThread(() =>
            {

                foreach (var role in runtime.Team)
                {
                    role.Recover();
                }
            });
        }

        static public void LightScence()
        {
            RunInMainThread(() =>
            {
                var blackCover = LevelMaster.Instance.transform.Find("UI/BlackCover");
                if (blackCover == null)
                {
                    Debug.LogError("DarkScence error，找不到LevelMaster/UI/BlackCover");
                    Next();
                    return;
                }

                var img = blackCover.GetComponent<Image>();
                img.DOFade(0, 1).OnComplete(() =>
                {
                    blackCover.gameObject.SetActive(false);
                    Next();
                });
            });
            Wait();
        }


        static public bool JudgeMoney(int money)
        {
            return (runtime.GetItemCount(GameConst.MONEY_ID) >= money);
        }

        /// <summary>
        /// 添加（减少）物品，并显示提示
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="count">可为负数</param>
        static public void AddItem(int itemId, int count)
        {
            RunInMainThread(() => {
                var item = ConfigTable.Get<Jyx2Item>(itemId);
                if (item == null)
                {
                    Debug.LogError("调用了未定义的物品:" + itemId);
                    return;
                }

                if (count < 0)
                {
                    storyEngine.DisplayPopInfo("失去物品:" + item.Name + "×" + Math.Abs(count));
                }
                else
                {
                    storyEngine.DisplayPopInfo("得到物品:" + item.Name + "×" + Math.Abs(count));
                }

                runtime.AddItem(itemId, count);
            });
        }

        static public void SetScencePosition2(int x, int y)
        {
            //设置位置，没用了，调用jyx2_MovePlayer替代
        }

        //增加声望
        static public void AddRepute(int value)
        {
            RunInMainThread(() =>{
                runtime.Player.Shengwang = HSFrameWork.Common.Tools.Limit(runtime.Player.Shengwang + value, 0, GameConst.MAX_ROLE_SHENGWANG);
                storyEngine.DisplayPopInfo("增加声望:" + value);
                Next();
            });
            Wait();
        }

        //韦小宝商店
        static public void WeiShop()
        {
            RunInMainThread(() =>
            {
                //int shopId = Tools.GetRandomInt(0, 4);
                Jyx2_UIManager.Instance.ShowUI("ShopUIPanel", "", new Action(()=>{Next();}));
            });
			Wait();
        }

        static public void AskSoftStar()
        {
            RunInMainThread(() => {
                var eventLuaPath = "jygame/ka" + UnityEngine.Random.Range(801, 820).ToString();
                Jyx2.LuaExecutor.Execute(eventLuaPath, null);
            });
        }


        static public void instruct_57()
        {

        }

        #region 扩展函数
        static public void jyx2_ReplaceSceneObject(string scene,string path, string replace)
        {
            RunInMainThread(() =>
            {
                LevelMasterBooster level = GameObject.FindObjectOfType<LevelMasterBooster>();
                level.ReplaceSceneObject(scene, path, replace);
                Next();
            });
            Wait();
        }
		
		
		// add to handle indoor transport object
		// path: name of destination transform
		// parent: parent path of destination transform
		// target: "" mean transport player. otherwise, need the full path of transport object.
		// eahphone at 2021/6/5
        static public void jyx2_MovePlayer(string path, string parent="Level/Triggers", string target="")
        {
			RunInMainThread(() =>
            {
                var levelMaster = GameObject.FindObjectOfType<LevelMaster>();
				levelMaster.TransportToTransform(parent, path, target);
				Next();
            });
			Wait();
        }

        static public void jyx2_CameraFollow(string path)
        {
            RunInMainThread(() =>
            {
                var followObj = GameObject.Find(path);
                if (followObj == null)
                {
                    Debug.LogError("jyx2_CameraFollow 找不到物体,path=" + path);
                    Next();
                    return;
                }
                var cameraBrain = Camera.main.GetComponent<CinemachineBrain>();
                if (cameraBrain != null)
                {
                    var vcam = cameraBrain.ActiveVirtualCamera;
                    if (vcam != null)
                    {
                        vcam.Follow = followObj.transform;
                    }
                }

                Next();
            });
            Wait();
        }
        
        static public void jyx2_CameraFollowPlayer()
        {
            jyx2_CameraFollow("Level/Player");
        }

		//fromName:-1, 获取主角当前位置作为起始点
        static public void jyx2_WalkFromTo(int fromName, int toName) 
        {
            RunInMainThread(() =>
            {
                var fromObj = GameObject.Find("Level/Player");
				if(fromName!=-1){
					fromObj=GameObject.Find($"Level/NavigateObjs/{fromName}");
				}
                var toObj = GameObject.Find($"Level/NavigateObjs/{toName}");
                if (fromObj == null || toObj == null) 
                {
                    GameUtil.LogError("jyx2_CameraFollow 找不到navigate物体,name=" + fromName + "/" + toName);
                    Next();
                    return;
                }
                LevelMaster.Instance.PlayerWarkFromTo(fromObj.transform.position,toObj.transform.position, () =>
                {
                    Next();
                });
            });
            Wait();
        }

        /// <param name="playableDirector"></param>
        static private void TimeLineNext(PlayableDirector playableDirector)
        {
            Next();
        }

        enum TimeLinePlayMode
        {
            ExecuteNextEventOnPlaying = 0,
            ExecuteNextEventOnEnd = 1,
        }

        static Animator clonePlayer;

        private static float _timelineSpeed = 1;

        static public void jyx2_SetTimelineSpeed(float speed)
        {
            _timelineSpeed = speed;
        }
        
        static public void jyx2_PlayTimeline(string timelineName, int playMode, bool isClonePlayer, string tagRole = "")
        {
            RunInMainThread(() =>
            {
                var timeLineRoot = GameObject.Find("Timeline");
                var timeLineObj = timeLineRoot.transform.Find(timelineName);

                if (timeLineObj == null)
                {
                    Debug.LogError("jyx2_PlayTimeline 找不到Timeline,path=" + timelineName);
                    Next();
                    return;
                }

                timeLineObj.gameObject.SetActive(true);
                var playableDirector = timeLineObj.GetComponent<PlayableDirector>();

                if(playMode == (int)TimeLinePlayMode.ExecuteNextEventOnEnd)
                {
                    playableDirector.stopped += TimeLineNext;
                }
                else if (playMode == (int)TimeLinePlayMode.ExecuteNextEventOnPlaying)
                {
                    Next();
                }
                
                playableDirector.Play();

                //timeline播放速度
                if (_timelineSpeed != 1 && _timelineSpeed > 0)
                {
                    playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(_timelineSpeed);    
                }
                

                //没有指定对象，则默认为主角播放
                if (string.IsNullOrEmpty(tagRole) || tagRole == "PLAYER")
                {
                    //克隆主角来播放特殊剧情
                    if (isClonePlayer)
                    {
                        if (clonePlayer == null)
                        {
                            clonePlayer = GameObject.Instantiate(GameRuntimeData.Instance.Player.View.GetAnimator());
                            clonePlayer.runtimeAnimatorController = null;
                            GameRuntimeData.Instance.Player.View.gameObject.SetActive(false);
                        }

                        DoPlayTimeline(playableDirector, clonePlayer.gameObject);
                    }
                    //正常绑定当前主角播放
                    else
                    {
                        var bindingDic = playableDirector.playableAsset.outputs;
                        bindingDic.ForEach(delegate (PlayableBinding playableBinding)
                        {
                            if (playableBinding.outputTargetType == typeof(Animator))
                            {
                                playableDirector.SetGenericBinding(playableBinding.sourceObject, GameRuntimeData.Instance.Player.View.GetAnimator().gameObject);
                            }
                        });
                    }
                }
                else
                {
                    string objPath = "Level/" + tagRole;
                    GameObject obj = GameObject.Find(objPath);
                    DoPlayTimeline(playableDirector, obj.gameObject);
                }
                
                LevelMaster.Instance.SetPlayerCanController(false);
            });
            Wait();
        }

        static void DoPlayTimeline(PlayableDirector playableDirector, GameObject player)
        {
            player.SetActive(false);

            var bindingDic = playableDirector.playableAsset.outputs;
            bindingDic.ForEach(delegate (PlayableBinding playableBinding)
            {
                if (playableBinding.outputTargetType == typeof(Animator))
                {
                    if (playableBinding.sourceObject != null)
                    {
                        playableDirector.GetComponent<PlayableDirectorHelper>().BindPlayer(player);
                    }
                    playableDirector.SetGenericBinding(playableBinding.sourceObject, player);
                }
            });
        }

        static public void jyx2_StopTimeline(string timelineName)
        {
            RunInMainThread(() =>
            {
                var timeLineRoot = GameObject.Find("Timeline");
                var timeLineObj = timeLineRoot.transform.Find(timelineName);

                if (timeLineObj == null)
                {
                    Debug.LogError("jyx2_PlayTimeline 找不到Timeline,path=" + timelineName);
                    Next();
                    return;
                }

                var playableDiretor = timeLineObj.GetComponent<PlayableDirector>();
                playableDiretor.stopped -= TimeLineNext;
                timeLineObj.gameObject.SetActive(false);

                GameRuntimeData.Instance.Player.View.gameObject.SetActive(true);
                GameRuntimeData.Instance.Player.View.GetAnimator().transform.localPosition = Vector3.zero;
                GameRuntimeData.Instance.Player.View.GetAnimator().transform.localRotation = Quaternion.Euler(Vector3.zero);
                if(clonePlayer != null)
                {
                    GameObject.Destroy(clonePlayer.gameObject);
                }
                clonePlayer = null;

                playableDiretor.GetComponent<PlayableDirectorHelper>().ClearTempObjects();
                LevelMaster.Instance.SetPlayerCanController(true);
                Next();
            });
            Wait();
        }

        static public void jyx2_Wait(float duration)
        {
            RunInMainThread(() =>
            {
                Sequence seq = DOTween.Sequence();
                seq.AppendCallback(Next)
                .SetDelay(duration / _timelineSpeed);
            });
            Wait();
        }


        /// <summary>
        /// 切换角色动作
        ///
        /// 调用样例（胡斐居）
        /// jyx2_SwitchRoleAnimation("Level/NPC/胡斐", "Assets/BuildSource/AnimationControllers/打坐.controller")
        /// </summary>
        /// <param name="rolePath"></param>
        /// <param name="animationControllerPath"></param>
        static public void jyx2_SwitchRoleAnimation(string rolePath, string animationControllerPath)
        {
            Debug.Log("jyx2_SwitchRoleAnimation called");

            RunInMainThread(() =>
            {
                LevelMasterBooster level = GameObject.FindObjectOfType<LevelMasterBooster>();
                if (level == null)
                {
                    Debug.LogError("jyx2_SwitchRoleAnimation调用错误，找不到LevelMaster");
                    Next();
                    return;
                }

                level.ReplaceNpcAnimatorController("", rolePath, animationControllerPath);
                Next();
            });
            Wait();
        }

        static public void jyx2_FixMapObject(string key, string value)
        {
            RunInMainThread(() =>
            {
                runtime.KeyValues[key] = value;
                var objs = GameObject.FindObjectsOfType<FixWithGameRuntime>();
                if (objs != null)
                {
					foreach(var obj in objs)
					{
						if(key==obj.Flag)
							obj.Reload();
						else continue;
					}
                }
				LevelMasterBooster level = GameObject.FindObjectOfType<LevelMasterBooster>();
				level.RefreshSceneObjects();
                Next();
            });

            Wait();
        }
		
		static public bool jyx2_CheckBookAndRepute()
		{
			if(runtime.Player.Shengwang<200)
			{
				return false;
			}
			for(var i=144;i<158;i++)
			{
				if(!HaveItem(i))
				{
					return false;
				}
			}
			return true;
		}

        #endregion


        #region private

        static private void RunInMainThread(Action run)
        {
            Loom.QueueOnMainThread(_ =>
            {
                run();
            }, null);
        }

        /// <summary>
        /// 等待返回
        /// </summary>
        static private void Wait()
        {
            sema.WaitOne();
        }

        /// <summary>
        /// 下一条指令
        /// </summary>
        static private void Next()
        {
            sema.Release();
        }

        static private int _selectResult;

        static private bool ShowYesOrNoSelectPanel(string selectMessage)
        {
            RunInMainThread(() =>
            {
                List<string> selectionContent = new List<string>() { "是", "否" };
                storyEngine.BlockPlayerControl = true;
                //storyEngine.dialogPanel.ShowSelection("主角", selectMessage, selectionContent, delegate (int index)
                //{
                //    _selectResult = index;
                //    storyEngine.BlockPlayerControl = false;
                //    Next();
                //});
                Jyx2_UIManager.Instance.ShowUI("ChatUIPanel", ChatType.Selection, "主角", selectMessage, selectionContent, new Action<int>((index) =>
                {
                    _selectResult = index;
                    storyEngine.BlockPlayerControl = false;
                    Next();
                }));
            });

            Wait();
            return _selectResult == 0;
        }

        static private bool JudgeRoleValue(int roleId, Predicate<RoleInstance> judge)
        {
            bool result = false;
            RunInMainThread(() =>
            {
                var role = runtime.GetRoleInTeam(roleId);
				if(role == null)
				{
					role = runtime.GetRole(roleId);
				}
                if (role == null)
                {
                    Debug.LogError("调用了不存在的role，roleid=" + roleId);
                    result = false;
                    Next();
                    return;
                }

                result = judge(role);
                Next();
            });

            Wait();
            return result;
        }


        #endregion
    }
}
