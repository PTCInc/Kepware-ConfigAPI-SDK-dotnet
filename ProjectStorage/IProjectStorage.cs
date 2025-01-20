﻿using KepwareSync.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KepwareSync.ProjectStorage
{
    public record StorageChangeEvent
    {
        public enum ChangeType
        {
            added,
            removed,
            changed
        }
        public ChangeType Type { get; }
        public StorageChangeEvent(ChangeType type)
        {
            Type = type;
        }
    }

    public interface IProjectStorage
    {
        public Task<Project> LoadProject(bool blnLoadFullProject = true);
        public Task ExportProjecAsync(Project project);

        public IObservable<StorageChangeEvent> ObserveChanges();
    }
}
