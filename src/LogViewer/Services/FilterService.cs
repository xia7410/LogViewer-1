﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterService.cs" company="Wild Gums">
//   Copyright (c) 2008 - 2015 Wild Gums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace LogViewer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Catel;
    using Catel.Collections;
    using Catel.Services;
    using Models;

    internal class FilterService : IFilterService
    {
        #region Fields
        private static readonly object Sync = new object();
        private readonly IAggregateLogService _aggregateLogService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IIndexSearchService _indexSearchService;
        #endregion

        #region Constructors
        public FilterService(IIndexSearchService indexSearchService, IDispatcherService dispatcherService, IAggregateLogService aggregateLogService)
        {
            Argument.IsNotNull(() => indexSearchService);
            Argument.IsNotNull(() => dispatcherService);
            Argument.IsNotNull(() => aggregateLogService);

            _indexSearchService = indexSearchService;
            _dispatcherService = dispatcherService;
            _aggregateLogService = aggregateLogService;

            Filter = new Filter();
        }
        #endregion

        #region Properties
        public Filter Filter { get; set; }
        #endregion

        #region IFilterService Members
        private IEnumerable<LogRecord> FilterRecords(Filter filter, IEnumerable<FileNode> logFiles)
        {
            Argument.IsNotNull(() => filter);
            Argument.IsNotNull(() => logFiles);

            if (!filter.SearchTemplate.UseFullTextSearch || string.IsNullOrEmpty(filter.SearchTemplate.TemplateString))
            {
                return logFiles.Where(filter.IsAcceptableTo).SelectMany(file => file.Records).Where(record => filter.IsAcceptableTo(record.LogEvent) && filter.IsAcceptableTo(record.Message));
            }

            Func<LogRecord, bool> where = record => filter.IsAcceptableTo(record.LogEvent);
            return logFiles.Where(filter.IsAcceptableTo) // select only approriate files
                .SelectMany(file => _indexSearchService.Select(file, filter.SearchTemplate.TemplateString, where)) // select records and scores from each file
                .OrderBy(t => t.Item2) // sort by relevance
                .Select(t => t.Item1); // we don't need score anymore
        }

        public void ApplyFilesFilter(FileBrowserModel fileBrowser)
        {
            Argument.IsNotNull(() => fileBrowser);

            FilterSelectedFiles(fileBrowser);

            FilterAllFiles(fileBrowser);
        }

        public void ApplyLogRecordsFilter(FileBrowserModel fileBrowser)
        {
            Argument.IsNotNull(() => fileBrowser);

            lock (Sync)
            {
                var logRecords = _aggregateLogService.AggregateLog.Records;

                var oldRecords = logRecords.ToArray();
                _dispatcherService.Invoke(() => logRecords.ReplaceRange(FilterRecords(Filter, fileBrowser.SelectedItems.OfType<FileNode>())));

                foreach (var record in logRecords.Except(oldRecords))
                {
                    record.FileNode.IsExpanded = true;
                }
            }            
        }

        private void FilterSelectedFiles(FileBrowserModel logViewer)
        {
            Argument.IsNotNull(() => logViewer);

            var selectedItems = logViewer.SelectedItems;

            var buff = selectedItems.OfType<FileNode>().ToArray();
            if (buff.Any())
            {
                selectedItems.Clear();
                foreach (var file in buff)
                {
                    if (Filter.IsAcceptableTo(file))
                    {
                        selectedItems.Add(file);
                    }
                    else
                    {
                        file.IsSelected = false;
                        file.IsItemSelected = false;
                    }
                }
            }
        }

        private void FilterAllFiles(FileBrowserModel fileBrowser)
        {
            Argument.IsNotNull(() => fileBrowser);

            foreach (var file in fileBrowser.RootDirectories.SelectMany(x => x.GetAllNestedFiles()))
            {
                var filter = Filter;
                file.IsVisible = filter.IsAcceptableTo(file);
            }
        }
        #endregion
    }
}