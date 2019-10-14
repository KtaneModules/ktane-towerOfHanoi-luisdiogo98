using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class towerOfHanoi : MonoBehaviour 
{
	public KMBombInfo bomb;
	public KMAudio Audio;

	//Logging
	static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
	private bool animating = false;

	public KMSelectable[] btns;
	public GameObject[] disks;

	List<int> sol;
	List<int> altSol;
	Stack[] rods = new Stack[3];

	int slot = -1;

	void Awake()
	{
		moduleId = moduleIdCounter++;

		btns[0].OnInteract += delegate () { PressButton(0); return false; };
		btns[1].OnInteract += delegate () { PressButton(1); return false; };
		btns[2].OnInteract += delegate () { PressButton(2); return false; };
	}

	void PressButton(int btn)
	{
		if(moduleSolved || animating)
			return;

		if(slot == -1)
		{
			if(rods[btn].Count == 0)
				return;

			slot = (int) rods[btn].Pop();

			StartCoroutine(DiskUp(slot));
		}
		else
		{
			if(rods[btn].Count != 0 && slot > (int) rods[btn].Peek())
			{
            	GetComponent<KMBombModule>().HandleStrike();
				return;
			}

			rods[btn].Push(slot);
			slot = -1;

			
			List<int> statusRight = rods[2].Cast<int>().ToList();
			statusRight.Sort();

			List<int> statusCenter = rods[1].Cast<int>().ToList();
			statusCenter.Sort();

			if(statusRight.SequenceEqual(sol) && statusCenter.SequenceEqual(altSol))
			{
				moduleSolved = true;
				GetComponent<KMBombModule>().HandlePass();
			}

			StartCoroutine(DiskDown((int) rods[btn].Peek(), rods[btn].Count, btn));
		}
	}

	void Start() 
	{
		CalcSolution();
		FillRods();
	}

	void CalcSolution()
	{
		sol = new List<int>();

		int[] digits = bomb.GetSerialNumberNumbers().ToArray();

		for(int i = 0; i < digits.Length; i++)
			if(digits[i] >= 1 && digits[i] <= 6 && !sol.Contains(digits[i]))
				sol.Add(digits[i]);

		if(bomb.GetBatteryCount() >= 1 && bomb.GetBatteryCount() <= 6 && !sol.Contains(bomb.GetBatteryCount()))
			sol.Add(bomb.GetBatteryCount());

		if(bomb.GetIndicators().Count() >= 1 && bomb.GetIndicators().Count() <= 6 && !sol.Contains(bomb.GetIndicators().Count()))
			sol.Add(bomb.GetIndicators().Count());

		if(bomb.GetPortCount() >= 1 && bomb.GetPortCount() <= 6 && !sol.Contains(bomb.GetPortCount()))
			sol.Add(bomb.GetPortCount());

		sol.Sort();

		altSol = new List<int>();
		
		for(int i = 1; i <= 6; i++)
			if(!sol.Contains(i))
				altSol.Add(i);

        Debug.LogFormat("[Tower of Hanoi #{0}] Required disks on the rightmost rod for solve are: {1}.", moduleId, sol.Join(", "));
        Debug.LogFormat("[Tower of Hanoi #{0}] Required disks on the center rod for solve are: {1}.", moduleId, altSol.Join(", "));
	}

	void FillRods()
	{
		rods[0] = new Stack();
		rods[1] = new Stack();
		rods[2] = new Stack();

		rods[0].Push(6);
		rods[0].Push(5);
		rods[0].Push(4);
		rods[0].Push(3);
		rods[0].Push(2);
		rods[0].Push(1);
	}

	IEnumerator DiskUp(int disk)
	{
		animating = true;

        Audio.PlaySoundAtTransform("wood", transform);

		float delta = (0.06f - disks[disk - 1].transform.localPosition.z) / 10;

		for(int i = 0; i < 10; i++)
		{
			foreach(KMSelectable btn in btns)
				btn.transform.Find("ArrowButHL").localScale += new Vector3(0, -0.2f, 0);
			disks[disk - 1].transform.localPosition += new Vector3(0, 0, delta);
			yield return new WaitForSeconds(0.0125f);
		}

		animating = false;
	}

	IEnumerator DiskDown(int disk, int height, int target)
	{
		animating = true;

		float delta = ((-0.047f + 0.047f * target) - (disks[disk - 1].transform.localPosition.x)) / 10;

		for(int i = 0; i < 10; i++)
		{
			disks[disk - 1].transform.localPosition += new Vector3(delta, 0, 0);
			yield return new WaitForSeconds(0.0125f);
		}

        Audio.PlaySoundAtTransform("wood", transform);

		delta = (-0.036f - 0.06f + 0.01f * (height - 1)) / 10;

		for(int i = 0; i < 10; i++)
		{
			foreach(KMSelectable btn in btns)
				btn.transform.Find("ArrowButHL").localScale += new Vector3(0, 0.2f, 0);
			disks[disk - 1].transform.localPosition += new Vector3(0, 0, delta);
			yield return new WaitForSeconds(0.0125f);
		}

		animating = false;
	}

    //twitch plays
    private bool inputIsValid(string cmd)
    {
        int count = 0;
        char[] validchars = { '1', '2', '3' };
        for(int i = 0; i < cmd.Length; i++)
        { 
            if(count == 2)
            {
                count = 0;
                if(!cmd.ElementAt(i).Equals(' '))
                {
                    return false;
                }
            }
            else if (!validchars.Contains(cmd.ElementAt(i))){
                return false;
            }
            else
            {
                count++;
                if(cmd.Length-1 == i && count != 2)
                {
                    return false;
                }
            }
        }
        return true;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} move 1 2;3 1;2 3 [Moves the specified pillar's top disk to the specified pillar (chainable)] | 1 = leftmost pillar & 3 = rightmost pillar";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ', ';', ',');
        if (Regex.IsMatch(parameters[0], @"^\s*move\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if(parameters.Length >= 3)
            {
                int count = 1;
                for (int i = 2; i < parameters.Length; i++)
                {
                    if(count == 2)
                    {
                        count = 0;
                        parameters[1] += " ";
                    }
                    count++;
                    parameters[1] += parameters[i];
                }
                if (inputIsValid(parameters[1]))
                {
                    yield return null;
                    string[] temp = parameters[1].Split(' ');
                    for (int i = 0; i < temp.Length; i++)
                    {
                        int init = int.Parse(temp[i].ElementAt(0) + "");
                        int end = int.Parse(temp[i].ElementAt(1) + "");
                        init--;
                        end--;
                        btns[init].OnInteract();
                        while(animating == true) { yield return new WaitForSeconds(0.1f); }
                        btns[end].OnInteract();
                        while (animating == true) { yield return new WaitForSeconds(0.1f); }
                    }
                }
            }
            yield break;
        }
    }
}
