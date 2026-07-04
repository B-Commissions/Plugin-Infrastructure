using System;
using System.Threading.Tasks;
using BlueBeard.Database.Attributes;

namespace BlueBeard.Database;

/// <summary>
/// Dispatches lifecycle hooks for a DbSet operation. Hooks run in declaration order;
/// column-targeted hooks receive the entity's current value for their column.
/// </summary>
internal static class HookRunner
{
    public static async Task RunAsync(TableMetadata metadata, HookKind kind, object entity)
    {
        // Hooks is empty for the vast majority of entities — bail without allocation.
        var hooks = metadata.Hooks;
        for (var i = 0; i < hooks.Count; i++)
        {
            var hook = hooks[i];
            if (hook.Kind != kind) continue;

            object arg = null;
            if (hook.TargetColumn != null)
            {
                arg = hook.TargetColumn.PropertyInfo.GetValue(entity);
                // A null column value cannot be represented by a non-nullable value-type
                // parameter — skip rather than throw mid-operation.
                var paramType = hook.Method.GetParameters()[0].ParameterType;
                if (arg == null && paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                    continue;
            }

            await hook.Invoker(entity, arg);
        }
    }
}
