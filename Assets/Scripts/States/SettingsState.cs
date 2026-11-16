using System;
using Golf.States.Interfaces;
using Golf.View;
using UnityEngine;

namespace Golf.States
{
	public class SettingsState : IGameState, IActivate
	{
		private static readonly string[] s_presets = { "Mobile Low", "Mobile", "Mobile High" };

		private readonly StateMachine m_stateMachine;
		private readonly SettingsView m_settingsView;

		public SettingsState(StateMachine stateMachine, SettingsView settingsView)
		{
			m_stateMachine = stateMachine;
			m_settingsView = settingsView;
		}

		public void Enter()
		{
			string current = QualitySettings.names[QualitySettings.GetQualityLevel()];
			m_settingsView.SetPreset(Array.IndexOf(s_presets, current));
			m_settingsView.Show();
		}

		public void Exit()
		{
			if (m_settingsView != null)
			{
				m_settingsView.Hide();
			}
		}

		public void Activate()
		{
			m_settingsView.onPresetChanged += OnPresetChangedHandler;
			m_settingsView.onBack += OnBackHandler;
		}

		public void Deactivate()
		{
			m_settingsView.onPresetChanged -= OnPresetChangedHandler;
			m_settingsView.onBack -= OnBackHandler;
		}

		private void OnPresetChangedHandler(int index)
		{
			int qualityLevel = Array.IndexOf(QualitySettings.names, s_presets[index]);
			if (qualityLevel >= 0)
			{
				QualitySettings.SetQualityLevel(qualityLevel, true);
				m_settingsView.SetPreset(index);
			}
		}

		private void OnBackHandler()
		{
			m_stateMachine.PopState();
		}
	}
}
