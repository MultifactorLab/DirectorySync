 using DirectorySync.Domain.Entities;

 namespace DirectorySync.Application.Integrations.Ldap.Windows;

 public interface IGetReferenceGroupMembers
 {
     IEnumerable<ReferenceDirectoryGroupMember> Execute(string groupDn, IEnumerable<string> requiredAttributes);
 }
