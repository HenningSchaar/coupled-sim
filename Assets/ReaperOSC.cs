using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace extOSC.Examples
{
	public class ReaperOSC : MonoBehaviour
	{
		#region Public Vars

		public string Address1 = "t/play";
        public string Address2 = "t/stop";
        public string Address3 = "f/time";

		[Header("OSC Settings")]
		public OSCTransmitter Transmitter;

		#endregion

		#region Unity Methods

		protected virtual void Start()
		{
            var rewindMessage = new OSCMessage(Address3);
			rewindMessage.AddValue(OSCValue.Float(0));

			Transmitter.Send(rewindMessage);

			var message = new OSCMessage(Address1);
			message.AddValue(OSCValue.String(" "));

			Transmitter.Send(message);
		}

        private void OnApplicationQuit()
        {
            var message = new OSCMessage(Address2);
			message.AddValue(OSCValue.String(" "));

			Transmitter.Send(message);
        }

		#endregion
	}
}