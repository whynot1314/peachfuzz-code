﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

using NLog;

using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom.XPath;
using System.Xml.Serialization;
using System.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Action types
	/// </summary>
	public enum ActionType
	{
		Unknown,

		Start,
		Stop,

		Accept,
		Connect,
		Open,
		Close,

		Input,
		Output,

		Call,
		SetProperty,
		GetProperty,

		ChangeState,
		Slurp
	}

	public delegate void ActionStartingEventHandler(Action action);
	public delegate void ActionFinishedEventHandler(Action action);

	/// <summary>
	/// Performs an Action such as sending output,
	/// calling a method, etc.
	/// </summary>
	[Serializable]
	public class Action : INamed, IPitSerializable
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public string _name = "Unknown Action";
		public ActionType type = ActionType.Unknown;

		public State parent = null;

		protected DataModel _dataModel;
		protected DataModel _origionalDataModel;
		protected DataSet _dataSet;

		protected List<ActionParameter> _params = new List<ActionParameter>();

		protected string _publisher = null;
		protected string _when = null;
		protected string _onStart = null;
		protected string _onComplete = null;
		protected string _ref = null;
		protected string _method = null;
		protected string _property = null;
		//protected string _value = null;
		protected string _setXpath = null;
		protected string _valueXpath = null;

		public string name
		{
			get { return _name; }
			set { _name = value; }
		}

		/// <summary>
		/// Data attached to action
		/// </summary>
		public DataSet dataSet
		{
			get { return _dataSet; }
			set { _dataSet = value; }
		}

		/// <summary>
		/// Current copy of the data model we are mutating.
		/// </summary>
		public DataModel dataModel
		{
			get { return _dataModel; }
			set
			{
				//if (_origionalDataModel == null)
				//{
				//    // Optimize output by generateing value
				//    object tmp = value.Value;

				//    _origionalDataModel = ObjectCopier.Clone<DataModel>(value);
				//    _origionalDataModel.action = this;
				//    _origionalDataModel.dom = null;

				//    _dataModel = value;
				//    _dataModel.action = this;
				//    _dataModel.dom = null;
				//}
				//else
				//{
					_dataModel = value;
					if (_dataModel != null)
					{
						_dataModel.action = this;
						_dataModel.dom = null;
					}
				//}
			}
		}

		/// <summary>
		/// Origional copy of the data model we will be mutating.
		/// </summary>
		public DataModel origionalDataModel
		{
			get { return _origionalDataModel; }
			set
			{
				_origionalDataModel = value;

				// Optimize output by pre-generating value
				object tmp = _origionalDataModel.Value;
			}
		}

		//public string value
		//{
		//    get { return _value; }
		//    set { _value = value; }
		//}

		/// <summary>
		/// Array of parameters for a method call
		/// </summary>
		public List<ActionParameter> parameters
		{
			get { return _params; }
			set { _params = value; }
		}

		/// <summary>
		/// xpath for selecting set targets during slurp.
		/// </summary>
		/// <remarks>
		/// Can return multiple elements.  All returned elements
		/// will be updated with a new value.
		/// </remarks>
		public string setXpath
		{
			get { return _setXpath; }
			set { _setXpath = value; }
		}

		/// <summary>
		/// xpath for selecting value during slurp
		/// </summary>
		/// <remarks>
		/// Must return a single element.
		/// </remarks>
		public string valueXpath
		{
			get { return _valueXpath; }
			set { _valueXpath = value; }
		}

		/// <summary>
		/// Name of publisher to use
		/// </summary>
		public string publisher
		{
			get { return _publisher; }
			set { _publisher = value; }
		}

		/// <summary>
		/// Only run action when expression is true
		/// </summary>
		public string when
		{
			get { return _when; }
			set { _when = value; }
		}

		/// <summary>
		/// Expression to run when action is starting
		/// </summary>
		public string onStart
		{
			get { return _onStart; }
			set { _onStart = value; }
		}

		/// <summary>
		/// Expression to run when action is completed
		/// </summary>
		public string onComplete
		{
			get { return _onComplete; }
			set { _onComplete = value; }
		}

		/// <summary>
		/// Name of state to change to, type=ChangeState
		/// </summary>
		public string reference
		{
			get { return _ref; }
			set { _ref = value; }
		}

		/// <summary>
		/// Method to call
		/// </summary>
		public string method
		{
			get { return _method; }
			set { _method = value; }
		}

		/// <summary>
		/// Property to operate on
		/// </summary>
		public string property
		{
			get { return _property; }
			set { _property = value; }
		}


		/// <summary>
		/// Action is starting to execute
		/// </summary>
		public static event ActionStartingEventHandler Starting;
		/// <summary>
		/// Action has finished executing
		/// </summary>
		public static event ActionFinishedEventHandler Finished;

		protected virtual void OnStarting()
		{
			if(!string.IsNullOrEmpty(onStart))
			{
				Dictionary<string, object> state = new Dictionary<string, object>();
				state["action"] = this;
				state["state"] = this.parent;
				state["self"] = this;

				Scripting.EvalExpression(onStart, state);
			}

			if (Starting != null)
				Starting(this);
		}

		protected virtual void OnFinished()
		{
			if (!string.IsNullOrEmpty(onComplete))
			{
				Dictionary<string, object> state = new Dictionary<string, object>();
				state["action"] = this;
				state["state"] = this.parent;
				state["self"] = this;

				Scripting.EvalExpression(onComplete, state);
			}

			if (Finished != null)
				Finished(this);
		}

		/// <summary>
		/// Update any DataModels we contain to new clones of
		/// origionalDataModel.
		/// </summary>
		/// <remarks>
		/// This should be performed in StateModel to every State/Action at
		/// start of the iteration.
		/// </remarks>
		public void UpdateToOrigionalDataModel()
		{
			switch (type)
			{
				case ActionType.Start:
				case ActionType.Stop:
				case ActionType.Open:
				case ActionType.Connect:
				case ActionType.Close:
				case ActionType.Accept:
				case ActionType.ChangeState:
				case ActionType.Slurp:
					break;

				case ActionType.Input:
				case ActionType.Output:
				case ActionType.GetProperty:
				case ActionType.SetProperty:
					dataModel = origionalDataModel.Clone() as DataModel;
					dataModel.action = this;

					break;

				case ActionType.Call:
					foreach (ActionParameter p in this.parameters)
					{
						p.dataModel = p.origionalDataModel.Clone() as DataModel;
						p.dataModel.action = this;
					}

					// TODO - Also set ActionResult

					break;

				default:
					throw new ApplicationException("Error, Action.Run fell into unknown Action type handler!");
			}
		}

		public void Run(RunContext context)
		{
			logger.Trace("Run({0}): {1}", name, type);

			if (when != null)
			{
				Dictionary<string, object> state = new Dictionary<string, object>();
				state["action"] = this;
				state["Action"] = this;
				state["state"] = this.parent;
				state["State"] = this.parent;
				state["StateModel"] = this.parent.parent;
				state["Test"] = this.parent.parent.parent;
				state["self"] = this;

				object value = Scripting.EvalExpression(when, state);
				if (!(value is bool))
				{
					logger.Debug("Run: when return is not boolean: " + value.ToString());
					return;
				}

				if (!(bool)value)
				{
					logger.Debug("Run: when returned false");
					return;
				}
			}

			try
			{
				Publisher publisher = null;
				if (this.publisher != null && this.publisher != "Peach.Agent")
				{
					if (!context.test.publishers.ContainsKey(this.publisher))
					{
						logger.Debug("Run: Publisher '" + this.publisher + "' not found!");
						throw new PeachException("Error, Action '"+name+"' publisher value '" + this.publisher + "' was not found!");
					}

					publisher = context.test.publishers[this.publisher];
				}
				else
				{
					publisher = context.test.publishers[0];
				}

				OnStarting();

				switch (type)
				{
					case ActionType.Start:
						publisher.start(this);
						break;
					case ActionType.Stop:
						publisher.stop(this);
						break;
					case ActionType.Open:
					case ActionType.Connect:

						if (!publisher.HasStarted)
							publisher.start(this);

						publisher.open(this);
						break;
					case ActionType.Close:

						if (!publisher.HasStarted)
							publisher.start(this);

						publisher.close(this);
						break;

					case ActionType.Accept:

						if (!publisher.HasStarted)
							publisher.start(this);
						if (!publisher.IsOpen)
							publisher.open(this);

						publisher.accept(this);
						break;

					case ActionType.Input:
						logger.Debug("ActionType.Input");

						if (!publisher.HasStarted)
							publisher.start(this);
						if (!publisher.IsOpen)
							publisher.open(this);

						handleInput(publisher);
						parent.parent.dataActions.Add(this);
						break;
					case ActionType.Output:
						logger.Debug("ActionType.Output");

						if (!publisher.HasStarted)
							publisher.start(this);
						if (!publisher.IsOpen)
							publisher.open(this);

						publisher.output(this, new Variant(dataModel.Value));
						parent.parent.dataActions.Add(this);
						break;

					case ActionType.Call:

						if (!publisher.HasStarted)
							publisher.start(this);

						handleCall(publisher, context);
						parent.parent.dataActions.Add(this);
						break;
					case ActionType.GetProperty:

						if (!publisher.HasStarted)
							publisher.start(this);

						handleGetProperty(publisher);
						parent.parent.dataActions.Add(this);
						break;
					case ActionType.SetProperty:

						if (!publisher.HasStarted)
							publisher.start(this);

						handleSetProperty(publisher);
						parent.parent.dataActions.Add(this);
						break;

					case ActionType.ChangeState:
						logger.Debug("ActionType.ChangeState");
						handleChangeState();
						break;
					case ActionType.Slurp:
						logger.Debug("ActionType.Slurp");
						handleSlurp(context);
						break;

					default:
						throw new ApplicationException("Error, Action.Run fell into unknown Action type handler!");
				}
			}
			finally
			{
				OnFinished();
			}
		}

		protected void handleInput(Publisher publisher)
		{
			try
			{
				DataCracker cracker = new DataCracker();
				cracker.CrackData(dataModel, new IO.BitStream(publisher));
			}
			catch (CrackingFailure)
			{
				throw new SoftException();
			}
		}

		protected void handleCall(Publisher publisher, RunContext context)
		{
			// Are we sending to Agents?
			if (this.publisher == "Peach.Agent")
			{
				context.agentManager.Message("Action.Call", new Variant(this.method));

				Variant ret = new Variant(0);
				DateTime start = DateTime.Now;

				while (true)
				{
					ret = context.agentManager.Message("Action.Call.IsRunning", new Variant(this.method));
					if (ret != null && ((int)ret) == 0)
						break;

					// TODO - Expose 10 as the timeout
					if (DateTime.Now.Subtract(start).Seconds > 10)
						break;

					Thread.Sleep(200);
				}

				return;
			}

			publisher.call(this, method, parameters);
		}

		protected void handleGetProperty(Publisher publisher)
		{
			Variant result = publisher.getProperty(this, property);
			this.dataModel.DefaultValue = result;
		}

		protected void handleSetProperty(Publisher publisher)
		{
			publisher.setProperty(this, property, this.dataModel.InternalValue);
		}

		protected void handleChangeState()
		{
			if (!this.parent.parent.states.ContainsKey(reference))
			{
				logger.Debug("handleChangeState: Error, unable to locate state '" + reference + "'");
				throw new PeachException("Error, unable to locate state '" + reference + "' provided to action '" + name + "'");
			}

			logger.Debug("handleChangeState: Changing to state: " + reference);

			throw new ActionChangeStateException(this.parent.parent.states[reference]);
		}

		protected void handleSlurp(RunContext context)
		{
			PeachXPathNavigator navi = new PeachXPathNavigator(context.dom);
			var iter = navi.Select(valueXpath);
			if (!iter.MoveNext())
				throw new PeachException("Error, slurp valueXpath returned no values. [" + valueXpath + "]");

			DataElement valueElement = ((PeachXPathNavigator)iter.Current).currentNode as DataElement;
			if (valueElement == null)
				throw new PeachException("Error, slurp valueXpath did not return a Data Element. [" + valueXpath + "]");

			if (iter.MoveNext())
				throw new PeachException("Error, slurp valueXpath returned multiple values. [" + valueXpath + "]");

			iter = navi.Select(setXpath);

			if (!iter.MoveNext())
				throw new PeachException("Error, slurp setXpath returned no values. [" + setXpath + "]");

			do
			{
				var setElement = ((PeachXPathNavigator)iter.Current).currentNode as DataElement;
				if (setElement == null)
					throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + valueXpath + "]");

				logger.Debug("Slurp, setting " + setElement.fullName + " from " + valueElement.fullName);
				setElement.DefaultValue = valueElement.DefaultValue;
			}
			while (iter.MoveNext());
		}

    public XmlNode pitSerialize(XmlDocument doc, XmlNode parent)
    {
      XmlNode node = doc.CreateNode(XmlNodeType.Element, "Action", null);

      node.AppendAttribute("name", this.name);
      node.AppendAttribute("ref", this.reference);
      node.AppendAttribute("method", this.method);
      node.AppendAttribute("property", this.property);
      node.AppendAttribute("setXpath", this.setXpath);
      node.AppendAttribute("valueXpath", this.valueXpath);
      node.AppendAttribute("type", this.type.ToString());
      node.AppendAttribute("when", this.when);
      node.AppendAttribute("publisher", this.publisher);
      node.AppendAttribute("onStart", this.onStart);
      node.AppendAttribute("onComplete", this.onComplete);

      XmlSerializer xs;

      if (this.dataModel != null)
      {
        XmlNode eDataModel = doc.CreateElement("DataModel");
        eDataModel.AppendAttribute("ref", this.dataModel.name);
        node.AppendChild(eDataModel);
      }
      
      if (this.dataSet != null)
      {
        StringBuilder sb = new StringBuilder();
        StringWriter writer = new StringWriter(sb);
        xs = new XmlSerializer(typeof(DataSet));
        xs.Serialize(writer, this.dataSet);

        node.InnerXml = sb.ToString();
      }
      
      if (this.parameters != null)
      {
        foreach (ActionParameter ap in this.parameters)
        {
          node.AppendChild(ap.pitSerialize(doc, node));
        }
      }
      

      return node;
    }
  }

	public enum ActionParameterType
	{
		In,
		Out,
		InOut
	}

	public class ActionParameter : IPitSerializable
	{
		DataModel _origionalDataModel = null;
		DataModel _dataModel = null;

		public ActionParameterType type;
		public object data;

		public DataModel origionalDataModel
		{
			get { return _origionalDataModel; }
			set { _origionalDataModel = value; }
		}

		public DataModel dataModel
		{
			get { return _dataModel; }
			set
			{
				_dataModel = value;

				if (_origionalDataModel == null)
					_origionalDataModel =_dataModel.Clone() as DataModel;
			}
		}

    public XmlNode pitSerialize(XmlDocument doc, XmlNode parent)
    {
      throw new NotImplementedException();
    }
  }

	public class ActionResult
	{
		DataModel _origionalDataModel = null;
		DataModel _dataModel = null;

		public DataModel origionalDataModel
		{
			get { return _origionalDataModel; }
			set { _origionalDataModel = value; }
		}

		public DataModel dataModel
		{
			get { return _dataModel; }
			set
			{
				_dataModel = value;

				if (_origionalDataModel == null)
					_origionalDataModel = _dataModel.Clone() as DataModel;
			}
		}
	}

	public class ActionChangeStateException : Exception
	{
		public State changeToState;

		public ActionChangeStateException(State changeToState)
		{
			this.changeToState = changeToState;
		}
	}
}

// END
