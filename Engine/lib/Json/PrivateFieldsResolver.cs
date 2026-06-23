using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Engine;


public class PrivateFieldsResolver : DefaultContractResolver {
    protected override List<MemberInfo> GetSerializableMembers (Type objectType) {
        /// Get everything: public + private instance fields
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var members = objectType.GetMembers(flags);

        List<MemberInfo> serializable = new List<MemberInfo>();
        foreach (var member in members) {
            if (member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property) {
                serializable.Add(member);
            }
        }

        return serializable;
    }
}
