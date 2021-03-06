using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 管理全场的骰子
/// </summary>
public class ScoreManager : Singleton<ScoreManager>
{
    private int totalScore = 0;

    private AbstractRule rulesChain = BuildRulesChain();

    public int Score
    {
        get { return totalScore; }
    }
    private static Vector3[] slotPosition = SlotPosition();

    public Transform slotInitPos;

    public AudioClip[] winSound;
  

    /// <summary>
    /// 计分器清零
    /// </summary>

    
    public void InitScoreManager()
    {
        totalScore = 0;
    }

    private static Vector3[] SlotPosition()
    {
        Vector3[] res = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            res[i] = new Vector3(19.73743f, 4.460415f, 5.226065f - 2 * i);
        }
        return res;
    }

    /// <summary>
    /// 检查场上骰子阵型并算分，骰子落地和销毁都要触发
    /// </summary>
    public void CheckDiceInScene()
    {
        GameObject[] objArrayInScene = GameObject.FindGameObjectsWithTag("Dice");
        if (objArrayInScene == null || objArrayInScene.Length == 0)
        {
            return;
        }

        // 规则校验
        List<DiceController>[] diceStateArray = InitDiceCollection();
        foreach (var obj in objArrayInScene)
        {
            DiceController dice = obj.GetComponent<DiceController>();
            if (dice != null)
            {
                diceStateArray[dice.State - 1].Add(dice);
            }
        }
        AbstractRule checkResult = rulesChain.CheckRule(diceStateArray);

        // TODO 
        // 规则类型
        string ruleType = checkResult.GetRuleType();

        if(ruleType=="StraightDraw")
        {
            AudioManager.Instance.playwin(winSound[0]);

        }
        if(ruleType=="FlushDraw")
        {
            AudioManager.Instance.playwin(winSound[1]);

        }


        // 计算总分
        totalScore += checkResult.GetScore();
        Scorekeeper.Instance.UpdateScore(checkResult.GetScore());
        // 回收骰子
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GunController gun = player.GetComponent<GunController>();
        for (int i = 0; i < checkResult.diceIDList.Count; i++)
        {
            checkResult.diceIDList[i].MoveToShowSlot(slotPosition[i]);
        }

        // 责任链结果清空
        rulesChain.Clear();
    }

    private List<DiceController>[] InitDiceCollection()
    {
        List<DiceController>[] diceCollection = new List<DiceController>[6];
        for (int i = 0; i < 6; i++)
        {
            diceCollection[i] = new List<DiceController>();
        }
        return diceCollection;
    }

    // 构建校验责任链
    private static AbstractRule BuildRulesChain()
    {
        AbstractRule straightDrawRule = new StraightDrawRule();
        AbstractRule flushDrawRule = new FlushDrawRule();

        straightDrawRule.SetNextRule(flushDrawRule);

        return straightDrawRule;
    }
}

/// <summary>
/// 规则校验责任链父类
/// </summary>
public abstract class AbstractRule
{
    protected AbstractRule nextRule;

    public List<DiceController> diceIDList;

    public void SetNextRule(AbstractRule nextRule)
    {
        this.nextRule = nextRule;
    }

    /// <summary>
    /// 责任链调用入口
    /// </summary>
    /// <param name="diceStateArray">当前场上骰子状况</param>
    /// <returns>规则结果</returns>
    public AbstractRule CheckRule(List<DiceController>[] diceStateArray)
    {
        diceIDList = CheckSelfRule(diceStateArray);
        if (diceIDList.Count != 0 || nextRule == null)
        {
            return this;
        }
        else
        {
            return nextRule.CheckRule(diceStateArray);
        }
    }

    /// <summary>
    /// 责任链结果清空，避免内存泄漏
    /// </summary>
    public void Clear()
    {
        this.diceIDList = null;
        if (this.nextRule != null)
        {
            this.nextRule.Clear();
        }
    }

    /// <summary>
    /// 检查具体规则
    /// </summary>
    /// <param name="diceStateArray">当前场上骰子状况</param>
    /// <returns>符合规则的骰子ID</returns>
    public abstract List<DiceController> CheckSelfRule(List<DiceController>[] diceStateArray);

    // 返回分数
    public abstract int GetScore();

    public abstract string GetRuleType();
}

/// <summary>
/// 校验顺子规则
/// </summary>
public class StraightDrawRule : AbstractRule
{
    public override int GetScore()
    {
        if (this.diceIDList.Count < 5)
        {
            return 0;
        }
        else if (this.diceIDList.Count == 5)
        {
            return 40;
        }
        else
        {
            return 50;
        }
    }

    public override List<DiceController> CheckSelfRule(List<DiceController>[] diceStateArray)
    {
        List<DiceController> res = new List<DiceController>();
        List<int> stae = new List<int>();
        foreach (var diceState in diceStateArray)
        {
            if (diceState.Count != 0)
            {
                res.Add(diceState[0]);
                stae.Add(diceState[0].State);
            }
            else if (res.Count < 5)
            {
                res.Clear();
                stae.Clear();
            }
            else
            {
                break;
            }
        }

        return res.Count < 5 ? new List<DiceController>() : res;
    }

    public override string GetRuleType()
    {
        if (this.diceIDList.Count == 0)
        {
            return "NoRule";
        }
        else
        {
            return "StraightDraw";
        }
    }
}

/// <summary>
/// 校验同花规则
/// </summary>
public class FlushDrawRule : AbstractRule
{
    public override int GetScore()
    {
        if (this.diceIDList.Count < 4)
        {
            return 0;
        }
        else if (this.diceIDList.Count == 4)
        {
            return 40;
        }
        else if (this.diceIDList.Count == 5)
        {
            return 50;
        }
        else
        {
            return 60;
        }
    }

    public override List<DiceController> CheckSelfRule(List<DiceController>[] diceStateArray)
    {
        int maxLen = 0;
        int maxLenIdx = -1;
        for (int i = 0; i < diceStateArray.Length; i++)
        {
            if (diceStateArray[i].Count > maxLen)
            {
                maxLen = diceStateArray[i].Count;
                maxLenIdx = i;
            }
        }

        if (maxLen >= 4)
        {
            return diceStateArray[maxLenIdx];
        }

        return new List<DiceController>();
    }

    public override string GetRuleType()
    {
        if (this.diceIDList.Count == 0)
        {
            return "NoRule";
        }
        else
        {
            return "FlushDraw";
        }
    }
}

/// <summary>
/// 校验两对规则
/// </summary>
public class DoublePairsRule : AbstractRule
{
    public override int GetScore()
    {
        if (this.diceIDList.Count == 0)
        {
            return 0;
        }
        else
        {
            return 20;
        }
    }

    public override List<DiceController> CheckSelfRule(List<DiceController>[] diceStateArray)
    {
        List<DiceController> res = new List<DiceController>();
        foreach (var diceState in diceStateArray)
        {
            if (diceState.Count == 2)
            {
                res.AddRange(diceState);
            }

            if (res.Count >= 4)
            {
                return res;
            }
        }

        return new List<DiceController>();
    }

    public override string GetRuleType()
    {
        if (this.diceIDList.Count == 0)
        {
            return "NoRule";
        }
        else
        {
            return "DoublePairs";
        }
    }
}
