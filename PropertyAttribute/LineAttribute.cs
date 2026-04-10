using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LineCustomAttribute : PropertyAttribute
{
    public const float HEIGHT = 1.25f;
    public const float OPACITY = 0.3f;

    public readonly float TopPadding;
    public readonly float BottomPadding;
    public readonly bool IsDashed;
    public readonly string HeaderText;

#if UNITY_2023_3_OR_NEWER || UNITY_6000_0_OR_NEWER
    public LineCustomAttribute(
        float topPadding = 10f,
        float bottomPadding = 10f,
        bool isDashed = false,
        string headerText = null
    ) : base(true)
#else
    public LineCustomAttribute(
        float topPadding = 10f,
        float bottomPadding = 10f,
        bool isDashed = false,
        string headerText = null
    )
#endif
    {
        TopPadding = topPadding;
        BottomPadding = bottomPadding;
        IsDashed = isDashed;
        HeaderText = headerText;
    }
}

public class LineTitleAttribute : LineCustomAttribute
{
    public LineTitleAttribute(string headerText = null)
        : base(20f, 3f, false, headerText) { }
}

public class LineSubtitleAttribute : LineCustomAttribute
{
    public LineSubtitleAttribute(string headerText = null)
        : base(10f, 3f, true, headerText) { }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LineCustomAttribute), true)]
public class LineDrawer : PropertyDrawer
{
    private const float HeaderBottomSpacing = 5f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (LineCustomAttribute)attribute;
        GUIContent actualLabel = EditorGUI.BeginProperty(position, label, property);

        float y = position.y;

        float lineHeight = GetLineBlockHeight(attr);
        float headerHeight = HasHeader(attr)
            ? EditorGUIUtility.singleLineHeight + HeaderBottomSpacing
            : 0f;
        float propertyHeight = EditorGUI.GetPropertyHeight(property, actualLabel, true);

        // 0) Line
        Rect lineRect = new Rect(
            position.x,
            y,
            position.width,
            lineHeight
        );
        DrawLine(lineRect, attr);
        y += lineHeight;

        // 1) Header
        if (HasHeader(attr))
        {
            Rect headerRect = new Rect(
                position.x,
                y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );

            EditorGUI.LabelField(headerRect, attr.HeaderText, EditorStyles.boldLabel);
            y += headerHeight;
        }

        // 2) Actual field
        Rect fieldRect = new Rect(
            position.x,
            y,
            position.width,
            propertyHeight
        );

        // List / Array는 label 없는 오버로드가 더 안정적
        if (IsCollectionProperty(property))
        {
            EditorGUI.PropertyField(fieldRect, property, true);
        }
        else
        {
            EditorGUI.PropertyField(fieldRect, property, actualLabel, true);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (LineCustomAttribute)attribute;

        float total = 0f;
        total += GetLineBlockHeight(attr);

        if (HasHeader(attr))
            total += EditorGUIUtility.singleLineHeight + HeaderBottomSpacing;

        total += EditorGUI.GetPropertyHeight(property, label, true);
        return total;
    }

    private static bool HasHeader(LineCustomAttribute attr)
    {
        return !string.IsNullOrEmpty(attr.HeaderText);
    }

    private static bool IsCollectionProperty(SerializedProperty property)
    {
        return property.isArray && property.propertyType != SerializedPropertyType.String;
    }

    private static float GetLineBlockHeight(LineCustomAttribute attr)
    {
        return attr.TopPadding + LineCustomAttribute.HEIGHT + attr.BottomPadding;
    }

    private static void DrawLine(Rect area, LineCustomAttribute attr)
    {
        float y = area.y + attr.TopPadding;
        Color lineColor = new Color(0.5f, 0.5f, 0.5f, LineCustomAttribute.OPACITY);

        if (attr.IsDashed)
        {
            const float dashWidth = 4f;
            const float dashSpace = 3f;

            for (float x = area.xMin; x < area.xMax; x += dashWidth + dashSpace)
            {
                float currentDashWidth = Mathf.Min(dashWidth, area.xMax - x);
                EditorGUI.DrawRect(
                    new Rect(x, y, currentDashWidth, LineCustomAttribute.HEIGHT),
                    lineColor
                );
            }
        }
        else
        {
            EditorGUI.DrawRect(
                new Rect(area.xMin, y, area.width, LineCustomAttribute.HEIGHT),
                lineColor
            );
        }
    }
}
#endif