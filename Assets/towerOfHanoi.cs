using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

			if(btn == 2)
			{
				List<int> status = rods[2].Cast<int>().ToList();
				status.Sort();

				if(status.SequenceEqual(sol))
				{
					moduleSolved = true;
            		GetComponent<KMBombModule>().HandlePass();
				}
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
			if(digits[i] >= 1 && digits[i] <= 5 && !sol.Contains(digits[i]))
				sol.Add(digits[i]);

		if(bomb.GetBatteryCount() >= 1 && bomb.GetBatteryCount() <= 5 && !sol.Contains(bomb.GetBatteryCount()))
			sol.Add(bomb.GetBatteryCount());

		if(bomb.GetIndicators().Count() >= 1 && bomb.GetIndicators().Count() <= 5 && !sol.Contains(bomb.GetIndicators().Count()))
			sol.Add(bomb.GetIndicators().Count());

		if(bomb.GetPortCount() >= 1 && bomb.GetPortCount() <= 5 && !sol.Contains(bomb.GetPortCount()))
			sol.Add(bomb.GetPortCount());

		if(sol.Count() == 0)
		{
			sol = new int[] { 1, 2, 3, 4, 5 }.ToList();
		}

		sol.Sort();

        Debug.LogFormat("[Tower of Hanoi #{0}] Required disks for solve are: {1}.", moduleId, sol.Join(", "));
	}

	void FillRods()
	{
		rods[0] = new Stack();
		rods[1] = new Stack();
		rods[2] = new Stack();

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
}
