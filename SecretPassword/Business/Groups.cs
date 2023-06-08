using Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business
{
    public static class Groups
    {
        private static IList<Group> groups { get; set; }

        public static IList<Group> LoadGroupsFromLocal()
        {
            groups = JsonConvert.DeserializeObject<IList<Group>>(Helpers.ReadGroups());

            if (groups == null)
                groups = new List<Group>();

            return groups;
        }

        private static void CheckGroupsLoaded()
        {
            if (groups != null)
                return;

            LoadGroupsFromLocal();
        }

        public static int Add(string newGroupName, string newGroupNotes)
        {
            CheckGroupsLoaded();

            if (string.IsNullOrEmpty(newGroupName))
                throw new Exception("Il nome del gruppo non può essere vuoto.");

            if (groups.Any(g => g.Name.ToLower() == newGroupName))
                throw new Exception("I nomi dei gruppi non possono essere uguali.");

            int proxID = 1;
            if (groups.Count > 0)
                proxID = groups.Max(g => g.ID) + 1;

            Group newGroup = new Group();
            newGroup.ID = proxID;
            newGroup.Name = newGroupName;
            newGroup.Notes = newGroupNotes;
            groups.Add(newGroup);

            groups = groups.OrderBy(g => g.Name).ToList();

            Save();

            return proxID;
        }

        private static void Save()
        {
            Helpers.SaveGroups(JsonConvert.SerializeObject(groups));
        }

        public static Group GetById(int newGroupID)
        {
            CheckGroupsLoaded();

            return groups.FirstOrDefault(g => g.ID == newGroupID);
        }

        public static void Delete(int groupID)
        {
            groups.Remove(groups.FirstOrDefault(c => c.ID == groupID));
            Save();
        }

        public static void Modify(int modifyGroupID, string newGroupName, string newGroupNotes)
        {
            Group group = groups.FirstOrDefault(g => g.ID == modifyGroupID);
            if (group != null)
            {
                if (string.IsNullOrEmpty(newGroupName))
                    throw new Exception("Il nome del gruppo non può essere vuoto.");

                if (groups.Any(g => g.Name.ToLower() == newGroupName && g.ID != group.ID))
                    throw new Exception("I nomi dei gruppi non possono essere uguali.");

                group.Name = newGroupName;
                group.Notes = newGroupNotes;
                groups.Remove(group);
                groups.Add(group);

                groups = groups.OrderBy(g => g.Name).ToList();
            }
            Save();
        }
    }
}
