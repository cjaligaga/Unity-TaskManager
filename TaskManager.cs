/// TaskManager.cs
/// Copyright (c) 2011, Ken Rockot  <k-e-n-@-REMOVE-CAPS-AND-HYPHENS-oz.gs>.  All rights reserved.
/// Everyone is granted non-exclusive license to do anything at all with this code.
///
/// 5 years later, the derivation from this code, done by Wildan Mubarok <http://wellosoft.wordpress.com>
/// Everyone is still granted non-exclusive license to do anything at all with this code.
///
/// This is a new coroutine interface for Unity.
///
/// The motivation for this is twofold:
///
/// 1. The existing coroutine API provides no means of stopping specific
///    coroutines; StopCoroutine only takes a string argument, and it stops
///    all coroutines started with that same string; there is no way to stop
///    coroutines which were started directly from an enumerator.  This is
///    not robust enough and is also probably pretty inefficient.
///
/// 2. StartCoroutine and friends are MonoBehaviour methods.  This means
///    that in order to start a coroutine, a user typically must have some
///    component reference handy.  There are legitimate cases where such a
///    constraint is inconvenient.  This implementation hides that
///    constraint from the user.
///
/// And another two benefit by using my derivation code including:
///
/// 1. It's Garbage-Collection free; the original workflow uses New() keyword
///    for every new Coroutiones, which means that after that coroutine finished,
///    This class isn't get recycled, resulting a new GC allocation. This also means
///    that by using this derived-code, resulting an increase in performance 
///    on both editor and real devices.
///
/// 2. the Implementation by IEnumerable isn't efficient enough if it used for many times.
///    Most of the time, we use Corountines for tweening a parameter. And to create one,
///    Takes a time to think the math behind the scene. This is C#, and there's why delegates 
///    exist. With this code, You can implement Delegate (especially it's ground breaking feature,
///    Anonymous method, google for that) to wrap up several lines to just a single line, while
///    this class handle the math for the time function. See below for futher example.
///
/// Example usage:
///
/// ----------------------------------------------------------------------------
/// IEnumerator MyAwesomeTask()
/// {
///     while(true) {
///         Debug.Log("Logcat iz in ur consolez, spammin u wif messagez.");
///         yield return null;
////    }
/// }
///
/// IEnumerator TaskKiller(float delay, Task t)
/// {
///     yield return new WaitForSeconds(delay);
///     t.Stop();
/// }
///
/// void SomeCodeThatCouldBeAnywhereInTheUniverse()
/// {
///     Task spam = new Task(MyAwesomeTask());
///     new Task(TaskKiller(5, spam));
/// }
/// ----------------------------------------------------------------------------
///
/// When SomeCodeThatCouldBeAnywhereInTheUniverse is called, the debug console
/// will be spammed with annoying messages for 5 seconds.
///
/// Simple, really.  There is no need to initialize or even refer to TaskManager.
/// When the first Task is created in an application, a "TaskManager" GameObject
/// will automatically be added to the scene root with the TaskManager component
/// attached.  This component will be responsible for dispatching all coroutines
/// behind the scenes.
///
/// Task also provides an event that is triggered when the coroutine exits.
///
/// ----------------------------------------------------------------------------
/// 
/// And as a said before, constant use of New() keyword will result on a significant
/// GC Allocations. this problem can be addressed by reusing the class after it's
/// current Coroutine finished. This is somewhat complex in theory, but, don't worry,
/// Here we use the implemention of a Class Pooler from what I'm use for my TEXDraw package.
/// To make the optimization takes effect, just replace from "new Task" to "Task.Get":
///
/// void SomeCodeThatCouldBeAnywhereInTheUniverse()
/// {
///     Task spam = Task.Get(MyAwesomeTask());
///     Task.Get(TaskKiller(5, spam));
/// }

using UnityEngine;
using System.Collections;
using TexDrawLib;

/// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
/// It is an error to attempt to start a task that has been stopped or which has
/// naturally terminated.
public class Task : IFlushable
{
	/// Returns true if and only if the coroutine is running. Paused tasks
	/// are considered to be running.
	public bool Running {
		get {
			return task.Running;
		}
	}

	/// Returns true if and only if the coroutine is currently paused.
	public bool Paused {
		get {
			return task.Paused;
		}
	}

	/// Determine whether this task is used once, or false if not
	/// If set to false, then it's your responsibility to call Flush() if this class is no longer use.
	public bool flushAtFinish = true;

	/// Delegate for termination subscribers.  manual is true if and only if
	/// the coroutine was stopped with an explicit call to Stop().
	public delegate void FinishedHandler (bool manual);

	/// Termination event.  Triggered when the coroutine completes execution.
	public event FinishedHandler Finished;

	/// Creates a new Task object for the given coroutine.
	///
	/// If autoStart is true (default) the task is automatically started
	/// upon construction.

	public Task()
	{}

	public Task (IEnumerator c, bool autoStart = true)
	{
		task = TaskManager.CreateTask (c);
		task.Finished += TaskFinished;
		if (autoStart)
			Start ();
	}

	/// Don't use above, use this instead to get from unused task
	/// Preveting futher GC Allocates
	public static Task Get (IEnumerator c, bool autoStart = true)
	{
		Task t = ObjPool<Task>.Get ();
		if (t.task == null)
			t.task = TaskManager.CreateTask (c);
		else
			t.task.coroutine = c;
		t.task.Finished += t.TaskFinished;
		if (autoStart)
			t.Start ();
		return t;
	}

	/// Delegate variant, for the simplicity of a sake
	public static Task Get (CallBack c, float totalTime, bool autoStart = true)
	{
		return Get (Iterator (c, totalTime));
	}

	public static Task Get (CallBack c, float totalTime, InterpolationType interpolType, bool autoStart = true)
	{
	 	return Get (Iterator (c, interpolType, totalTime));
	}

	public delegate void CallBack (float time);

	static IEnumerator Iterator (CallBack call, float totalTim)
	{
		float tim = Time.time + totalTim;
		while (tim > Time.time) {
			//The time that returns is normalized between 0...1
			call (1 - ((tim - Time.time) / totalTim));
			yield return null;
		}
	}

	static IEnumerator Iterator (CallBack call, InterpolationType interpolType, float totalTim)
	{
		float tim = Time.time + totalTim;
		while (tim > Time.time) {
			float t = 1 - ((tim - Time.time) / totalTim);
			switch (interpolType) {
			case InterpolationType.Linear:												break;
			case InterpolationType.InverseLinear:	t = 1 - t; 							break;
			case InterpolationType.SmoothStep:		t = Mathf.SmoothStep(0, 1, t);		break;
			case InterpolationType.SmootherStep:	t = t*t*t*(t * (6f*t-15f) + 10f);	break;
			case InterpolationType.Square:			t = Mathf.Sqrt(t);					break;
			case InterpolationType.Quadratic:		t = t * t;							break;
			case InterpolationType.Cubic:			t = t*t*t;							break;
			case InterpolationType.Sinerp:			t = Mathf.Sin(t * Mathf.PI / 2f);	break;
			case InterpolationType.Coserp:			t = 1-Mathf.Cos(t * Mathf.PI / 2f);	break;
			case InterpolationType.Circular:		t = 1-Mathf.Sqrt(1-t*t);			break;
			case InterpolationType.Random:			t = Random.value;					break;
			}
			//The time that returns is normalized between 0...1
			call (t);
			yield return null;
		}
	}

	/// Begins execution of the coroutine
	public void Start ()
	{
		task.Start ();
	}

	/// Discontinues execution of the coroutine at its next yield.
	public void Stop ()
	{
		task.Stop ();
	}

	public void Pause ()
	{
		task.Pause ();
	}

	public void Unpause ()
	{
		task.Unpause ();
	}

	void TaskFinished (bool manual)
	{
		FinishedHandler handler = Finished;
		if (handler != null)
			handler (manual);
		if (flushAtFinish)
			Flush ();
	}

	TaskManager.TaskState task;

	//Optimizer stuff ---------
	bool m_flushed;

	public bool GetFlushed ()
	{
		return m_flushed;
	}

	public void SetFlushed (bool flushed)
	{
		m_flushed = flushed;
	}

	public void Flush ()
	{
		task.Stop ();
		task.Finished -= TaskFinished;
		ObjPool<Task>.Release (this);
	}
    
}

class TaskManager : MonoBehaviour
{
	public class TaskState
	{
		public bool Running {
			get {
				return running;
			}
		}

		public bool Paused {
			get {
				return paused;
			}
		}

		public delegate void FinishedHandler (bool manual);

		public event FinishedHandler Finished;

		public IEnumerator coroutine;
		bool running;
		bool paused;
		bool stopped;

		public TaskState (IEnumerator c)
		{
			coroutine = c;
		}

		public void Pause ()
		{
			paused = true;
		}

		public void Unpause ()
		{
			paused = false;
		}

		public void Start ()
		{
			running = true;
			stopped = false;
			singleton.StartCoroutine (CallWrapper ());
		}

		public void Stop ()
		{
			stopped = true;
			running = false;
		}

		IEnumerator CallWrapper ()
		{
			yield return null;
			IEnumerator e = coroutine;
			while (running) {
				if (paused)
					yield return null;
				else {
					if (e != null && e.MoveNext ()) {
						yield return e.Current;
					} else {
						running = false;
					}
				}
			}
			
			FinishedHandler handler = Finished;
			if (handler != null)
				handler (stopped);
		}
	}

	static TaskManager singleton;

	public static TaskState CreateTask (IEnumerator coroutine)
	{
		if (singleton == null) {
			GameObject go = new GameObject ("TaskManager");
			singleton = go.AddComponent<TaskManager> ();
		}
		return new TaskState (coroutine);
	}
}

public enum InterpolationType
{
	Linear = 0,
	InverseLinear = 1,
	SmoothStep = 2,
	SmootherStep = 3,
	Sinerp = 4,
	Coserp = 5,
	Square = 6,
	Quadratic = 7,
	Cubic = 8,
	Exponential = 9,
	Circular = 10,
	Random = 11
}
