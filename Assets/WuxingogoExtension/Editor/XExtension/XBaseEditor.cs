using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

public class XBaseEditor : Editor
{

	public float CurrHeight = 0;
	public const float StartY = 45;
	public const float StartX = 10;
	public const float FieldOffset = 5;

	public sealed override void OnInspectorGUI()
	{
		GUILayout.Box( XResources.LogoTexture, GUILayout.Width( Screen.width - 40 ), GUILayout.Height( 100 ) );

		base.OnInspectorGUI();

		OnXGUI();
	}

	public virtual void OnXGUI()
	{
		
	}

	public bool CreateSpaceButton(string btnName)
	{
		return GUILayout.Button( btnName );
	}

	public void DoButton(string btnName, Action callback)
	{
		if( GUILayout.Button( btnName ) ) {
			callback();
		}
	}

	public void DoButton(GUIContent content, Action callback, params GUILayoutOption[] options)
	{
		if( GUILayout.Button( content, options ) ) {
			callback();
		}
	}

	public void DoButton<T>(string btnName, Action<T> callback, T arg)
	{
		if( GUILayout.Button( btnName ) ) {
			callback( arg );
		}
	}

	public void CreateSpaceBox()
	{
		GUILayout.Box( "", GUILayout.Width( Screen.width - 40 ), GUILayout.Height( 3 ) );
	}

	public bool CreateCheckBox(bool value)
	{
		return EditorGUILayout.Toggle( value );
	}

	public float CreateFloatField(float value)
	{
		return EditorGUILayout.FloatField( value );
	}

	public float CreateFloatField(string fieldName, float value)
	{
		return EditorGUILayout.FloatField( fieldName, value );
	}
	public Vector2 CreateVector2Field(string fieldName, Vector2 value)
	{
		return EditorGUILayout.Vector2Field (fieldName, value);
	}


	public Vector3 CreateVector3Field(string fieldName, Vector3 value)
	{
		return EditorGUILayout.Vector3Field (fieldName, value);
	}

	public Vector4 CreateVector4Field(string fieldName, Vector4 value)
	{
		return EditorGUILayout.Vector4Field (fieldName, value);
	}

	public Object CreateObjectField(string fieldName, Object obj, System.Type type = null)
	{
		if( null == type )
			type = typeof( Object );
		return EditorGUILayout.ObjectField( fieldName, obj, type, true ) as Object;
	}

	public Object CreateObjectField(Object obj, System.Type type = null)
	{
		if( null == type )
			type = typeof( Object );
		return EditorGUILayout.ObjectField( obj, type, true ) as Object;
	}

	public int CreateIntField(string fieldName, int value)
	{
		return EditorGUILayout.IntField( fieldName, value );
	}

	public int CreateIntField(int value)
	{
		return EditorGUILayout.IntField( value );
	}

	public long CreateLongField(long value)
	{
		return EditorGUILayout.LongField( value );
	}

	public string CreateStringField(string fieldName, string value)
	{
		return EditorGUILayout.TextField( fieldName, value );
	}

	public string CreateStringField(string value)
	{
		return EditorGUILayout.TextField( value );
	}

	public void CreateLabel(string fieldName)
	{
		EditorGUILayout.LabelField( fieldName );
	}

	public void CreateMessageField(string value, MessageType type)
	{
		EditorGUILayout.HelpBox( value, type );
		
	}

	public Enum CreateEnumSelectable(string fieldName, Enum value)
	{
		return EditorGUILayout.EnumPopup( fieldName, value );
	}

	public int CreateSelectableFromString(int rootID, string[] array)
	{
		return EditorGUILayout.Popup( array[rootID], rootID, array );
	}

	public void BeginHorizontal()
	{
		EditorGUILayout.BeginHorizontal();
	}

	public void EndHorizontal()
	{
		EditorGUILayout.EndHorizontal();
	}

	public void BeginVertical()
	{
		EditorGUILayout.BeginVertical();
	}

	public void EndVertical()
	{
		EditorGUILayout.EndVertical();
	}
}
