# AWS SAA-C03 Practice App — ProGuard / R8 rules
# Applied during Android Release builds to shrink and obfuscate the APK.

# Keep AWS SDK classes used via reflection
-keep class com.amazonaws.** { *; }
-keep class org.apache.commons.logging.** { *; }

# Keep SQLite-net model classes (used with reflection for table creation)
-keep class AwsSaaC03Practice.Models.** { *; }

# Keep JSON serialisation models
-keepclassmembers class * {
    @System.Text.Json.Serialization.JsonPropertyNameAttribute <fields>;
}

# LiveCharts / SkiaSharp
-keep class LiveChartsCore.** { *; }
-keep class SkiaSharp.** { *; }
