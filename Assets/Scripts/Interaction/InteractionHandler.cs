using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for systems that handle user input:

// Only one system can have focus at a time. If a system wants focus, it must call RequestFocus().
// The system that currently has focus will determine if it CanReleaseFocus().
// If yes:
// • FocusLost() will be called on that system.
// • Requesting system will have HasFocus set to true.

public abstract class InteractionHandler : MonoBehaviour {

	InteractionHandler[] allHandlers;
	bool hasFocus;

	public void InitAllHandlers (InteractionHandler[] allHandlers) {
		this.allHandlers = allHandlers;
	}

	public abstract void OrderedUpdate ();

	// Handle losing focus
	protected virtual void FocusLost () {

	}

	// Is this interaction handler willing to relinquish focus right now?
	protected virtual bool CanReleaseFocus () {
		return true;
	}

	// Request to have focus from whichever handler has focus at the moment.
	// If succesful, HasFocus will be set to true.
	protected virtual void RequestFocus () {
		if (!hasFocus) {
			bool noHandlersHaveFocus = true;
			foreach (var otherHandler in allHandlers) {
				if (otherHandler.hasFocus) {
					noHandlersHaveFocus = false;
					if (otherHandler.CanReleaseFocus ()) {
						otherHandler.hasFocus = false;
						otherHandler.FocusLost ();
						hasFocus = true;
						break;
					}

				}
			}

			if (noHandlersHaveFocus) {
				hasFocus = true;
			}
		}
	}

	// Does this system currently have focus?
	protected bool HasFocus {
		get {
			return hasFocus;
		}
	}
}