# -*- coding: utf-8 -*-
# Description: missanalyzer netdata python.d module
# Author: Will Kennedy (ThereGoesMySanity)
# SPDX-License-Identifier: GPL-3.0-or-later

from bases.FrameworkServices.SocketService import SocketService
import json

UNIX_SOCKET = '/run/missanalyzer-server'

ORDER = [
    'calls',
    'directCalls',
    'caches',
    'token',
    'apiv1',
    'apiv2',
    'downloads',
    'events',
    'servers',
    'cachedMessages',
    'beatmapsDb',
    'messages',
    'errors',
]

CHARTS = {
    'calls': {
        'options': [None, 'Calls', 'Bot calls/min', 'missanalyzer', 'missanalyzer.calls', 'line'],
        'lines': [
            ['BotDirectReqFalse', 'Requests denied', 'incremental', 60, 1],
            ['BotDirectReqTrue', 'Requests accepted', 'incremental', 60, 1],
            ['BotDirectResponse', 'Responses', 'incremental', 60, 1],
        ]
    },
    'directCalls': {
        'options': [None, 'Calls', 'Direct calls/min', 'missanalyzer', 'missanalyzer.directCalls', 'line'],
        'lines': [
            ['ErrorHandled', 'Handled', 'incremental', 60, 1],
            ['ErrorUnhandled', 'Unhandled', 'incremental', 60, 1],
        ]
    },
    'caches': {
        'options': [None, 'Database Caches', 'Cache calls/min', 'missanalyzer', 'missanalyzer.caches', 'line'],
        'lines': [
            ['BeatmapsCacheHit', 'BM Hits', 'incremental', 60, 1],
            ['BeatmapsCacheMiss', 'BM Misses', 'incremental', 60, 1],
            ['ReplaysCacheHit', 'Replay Hits', 'incremental', 60, 1],
            ['ReplaysCacheMiss', 'Replay Misses', 'incremental', 60, 1],
        ]
    },
    'token': {
        'options': [None, 'Token Expiry', 'minutes', 'missanalyzer', 'missanalyzer.api', 'line'],
        'lines': [
            ['TokenExpiry', 'Expiry', 'absolute'],
        ]
    },
    'apiv1': {
        'options': [None, 'API Calls (v1)', 'API Calls/min', 'missanalyzer', 'missanalyzer.api', 'line'],
        'lines': [
            ['ApiGetUserv1', 'User', 'incremental', 60, 1],
            ['ApiGetBeatmapsv1', 'Beatmaps', 'incremental', 60, 1],
        ]
    },
    'apiv2': {
        'options': [None, 'API Calls (v2)', 'API Calls/min', 'missanalyzer', 'missanalyzer.api', 'line'],
        'lines': [
            ['ApiGetUserScoresv2', 'User Scores', 'incremental', 60, 1],
            ['ApiGetBeatmapScoresv2', 'Beatmap Scores', 'incremental', 60, 1],
        ]
    },
    'downloads': {
        'options': [None, 'File Downloads', 'downloads/min', 'missanalyzer', 'missanalyzer.api', 'line'],
        'lines': [
            ['ApiDownloadBeatmap', 'Beatmap', 'incremental', 60, 1],
            ['ApiGetReplayv1', 'Replay', 'incremental', 60, 1],
        ]
    },
    'events': {
        'options': [None, 'Discord Events', 'events/min', 'missanalyzer', 'missanalyzer.events', 'line'],
        'lines': [
            ['EventsHandled', 'Events', 'incremental', 60, 1],
        ]
    },
    'servers': {
        'options': [None, 'Servers Joined', 'servers', 'missanalyzer', 'missanalyzer.servers', 'line'],
        'lines': [
            ['ServersJoined', 'Servers', 'absolute'],
        ]
    },
    'cachedMessages': {
        'options': [None, 'Cached Messages', 'messages', 'missanalyzer', 'missanalyzer.messages', 'line'],
        'lines': [
            ['CachedMessages', 'Messages', 'absolute'],
        ]
    },
    'beatmapsDb': {
        'options': [None, 'Beatmaps.db', 'beatmaps', 'missanalyzer', 'missanalyzer.caches', 'line'],
        'lines': [
            ['BeatmapsDbSize', 'Size', 'absolute'],
        ]
    },
    'messages': {
        'options': [None, 'Messages', 'messages/min', 'missanalyzer', 'missanalyzer.messages', 'line'],
        'lines': [
            ['MessageCreated', 'Message', 'incremental', 60, 1],
            ['HelpMessageCreated', 'Help', 'incremental', 60, 1],
            ['MessageEdited', 'Edited', 'incremental', 60, 1],
        ]
    },
    'errors': {
        'options': [None, 'Errors', 'errors/min', 'missanalyzer', 'missanalyzer.errors', 'line'],
        'lines': [
            ['ErrorHandled', 'Handled', 'incremental', 60, 1],
            ['ErrorUnhandled', 'Unhandled', 'incremental', 60, 1],
        ]
    },
}


class Service(SocketService):
    def __init__(self, configuration=None, name=None):
        SocketService.__init__(self, configuration=configuration, name=name)
        self.order = ORDER
        self.definitions = CHARTS
        self.host = None
        self.port = None
        self.unix_socket = UNIX_SOCKET
        # self._keep_alive = True
        self.request = 'GET all'

    def _get_data(self):
        """
        Format data received from socket
        :return: dict
        """
        try:
            raw = self._get_raw_data()
        except (ValueError, AttributeError):
            return None

        if raw is None:
            self.debug('missanalyzer returned no data')
            return None

        data = json.loads(raw)
        return data or None

    def _check_raw_data(self, data):
        return data.count('{') == data.count('}')