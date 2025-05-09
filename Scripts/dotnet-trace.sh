#!/bin/zsh

# dotnet-trace enhanced script with better heap visualization options
# Default values
DEFAULT_DURATION=30
DEFAULT_BUFFER_SIZE=1024
DEFAULT_MODE="heap"

# Help function
show_help() {
    echo "Enhanced .NET Heap Analysis Script"
    echo "---------------------------------"
    echo "Usage: $0 <scene-path> [options]"
    echo ""
    echo "Required:"
    echo "  <scene-path>               Path to Godot scene (e.g., res://my_scene.tscn)"
    echo ""
    echo "Options:"
    echo "  -d, --duration SECONDS     Duration to trace (default: $DEFAULT_DURATION)"
    echo "  -o, --output NAME          Output filename prefix (default: 'trace')"
    echo "  -b, --buffer SIZE          Buffer size in MB (default: $DEFAULT_BUFFER_SIZE)"
    echo "  -m, --mode MODE            Tracing mode (default: $DEFAULT_MODE)"
    echo "                             Modes: heap, gc, alloc, full"
    echo "  -h, --help                 Show this help message"
    exit 0
}

# Parse arguments
if [ $# -eq 0 ]; then
    show_help
fi

SCENE_PATH="$1"
shift

# Default values
DURATION=$DEFAULT_DURATION
OUTPUT_NAME="trace"
BUFFER_SIZE=$DEFAULT_BUFFER_SIZE
MODE=$DEFAULT_MODE

# Parse options
while (( "$#" )); do
    case "$1" in
        -h|--help)
            show_help
            ;;
        -d|--duration)
            DURATION="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_NAME="$2"
            shift 2
            ;;
        -b|--buffer)
            BUFFER_SIZE="$2"
            shift 2
            ;;
        -m|--mode)
            MODE="$2"
            shift 2
            ;;
        -*|--*=)
            echo "Error: Unsupported flag $1" >&2
            exit 1
            ;;
        *)
            shift
            ;;
    esac
done

# Create a timestamped log directory
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_DIR="${OUTPUT_NAME}_${TIMESTAMP}"
mkdir -p "$LOG_DIR"

# Set provider string based on mode - Focusing on heap visualization options
case "$MODE" in
    "heap")
        # General heap overview with allocation tracking
        PROVIDERS="Microsoft-Windows-DotNETRuntime:0x1:5,Microsoft-Windows-DotNETRuntime-Memory:0xC0:5"
        echo "Mode: HEAP - Tracking object allocations and heap structure"
        ;;
    "gc")
        # Detailed GC events including reasons and durations
        PROVIDERS="Microsoft-Windows-DotNETRuntime:0x1:5"
        echo "Mode: GC - Detailed garbage collection timing and reasons"
        ;;
    "alloc")
        # Focus on allocation sampling for allocation-heavy applications
        PROVIDERS="Microsoft-DotNETCore-SampleProfiler:0x0:4"
        echo "Mode: ALLOC - Sampling allocations to find hotspots"
        ;;
    "full")
        # Everything - but will generate large traces
        PROVIDERS="Microsoft-Windows-DotNETRuntime:0x1FFF:5,Microsoft-DotNETCore-SampleProfiler:0xF:4"
        echo "Mode: FULL - All events including allocations, GC, types, and exceptions"
        ;;
    *)
        echo "Error: Unknown mode $MODE. Using 'heap' mode instead."
        PROVIDERS="Microsoft-Windows-DotNETRuntime:0x1:5,Microsoft-Windows-DotNETRuntime-Memory:0xC0:5"
        ;;
esac

echo "Starting Godot with scene: $SCENE_PATH"
echo "Will trace for $DURATION seconds"
echo "Buffer size: $BUFFER_SIZE MB"
echo "Results will be saved to: $LOG_DIR/"

# Start Godot
nohup godot-mono "$SCENE_PATH" > "$LOG_DIR/godot_trace_run.log" 2>&1 &
GODOT_PID=$!

echo "Godot started with PID: $GODOT_PID"

# Wait a moment and check if Godot is still running
sleep 3
if ! ps -p $GODOT_PID > /dev/null; then
    echo "Error: Godot process exited prematurely. Check $LOG_DIR/godot_trace_run.log for details."
    cat "$LOG_DIR/godot_trace_run.log"
    exit 1
fi

# Create initial heap snapshot if dotnet-gcdump is available
if command -v dotnet-gcdump &> /dev/null; then
    echo "Taking initial heap snapshot..."
    dotnet-gcdump collect --process-id $GODOT_PID --output "$LOG_DIR/initial_heap.gcdump"
    echo "Initial heap snapshot saved"
fi

# Start trace collection
echo "Starting dotnet-trace collection..."
TRACE_OUTPUT="$LOG_DIR/$OUTPUT_NAME"

# Collect the trace
dotnet-trace collect \
    --process-id $GODOT_PID \
    --providers "$PROVIDERS" \
    --format Speedscope \
    --output "$TRACE_OUTPUT.nettrace" \
    --buffersize $BUFFER_SIZE \
    --duration "00:00:$DURATION" &
TRACE_PID=$!

# Wait for the specified duration plus a little buffer
echo "Tracing for $DURATION seconds..."
sleep $(($DURATION + 3))

# Check if Godot is still running
if ps -p $GODOT_PID > /dev/null; then
    # Create final heap snapshot if dotnet-gcdump is available
    if command -v dotnet-gcdump &> /dev/null; then
        echo "Taking final heap snapshot..."
        dotnet-gcdump collect --process-id $GODOT_PID --output "$LOG_DIR/final_heap.gcdump"
        echo "Final heap snapshot saved"
        
        # Generate size comparison file for before/after
        echo "Heap Snapshot Analysis" > "$LOG_DIR/heap_comparison.txt"
        echo "=====================" >> "$LOG_DIR/heap_comparison.txt"
        echo "Initial Heap Size: $(ls -lh "$LOG_DIR/initial_heap.gcdump" | awk '{print $5}')" >> "$LOG_DIR/heap_comparison.txt"
        echo "Final Heap Size: $(ls -lh "$LOG_DIR/final_heap.gcdump" | awk '{print $5}')" >> "$LOG_DIR/heap_comparison.txt"
    fi

    # Kill the Godot process
    echo "Stopping Godot..."
    kill $GODOT_PID 2>/dev/null
    
    # Check if the trace files were created
    if [ -f "$TRACE_OUTPUT.nettrace" ]; then
        echo "Trace collected successfully in $LOG_DIR/"
        
        # Convert to Speedscope format
        echo "Converting trace to Speedscope format..."
        dotnet-trace convert "$TRACE_OUTPUT.nettrace" --format Speedscope --output "$TRACE_OUTPUT.speedscope.json"
        
        # Create a basic analysis script for understanding allocations
        cat > "$LOG_DIR/allocation_analysis.html" << 'EOL'
<!DOCTYPE html>
<html>
<head>
    <title>Memory Allocation Analyzer</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }
        .container { max-width: 1200px; margin: 0 auto; }
        .info { background-color: #f0f0f0; padding: 15px; border-radius: 5px; margin-bottom: 20px; }
        .error { background-color: #ffeeee; color: #cc0000; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .success { background-color: #eeffee; color: #00aa00; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .loading { font-style: italic; color: #666; }
        table { width: 100%; border-collapse: collapse; margin-top: 15px; }
        th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background-color: #f2f2f2; }
        tr:hover { background-color: #f5f5f5; }
        .alloc-row { background-color: #ffeeee; }
        #debugOutput { 
            background-color: #f8f8f8; 
            border: 1px solid #ddd; 
            padding: 10px; 
            white-space: pre-wrap; 
            font-family: monospace; 
            font-size: 12px; 
            max-height: 200px; 
            overflow: auto; 
            display: none; 
        }
        .debug-toggle { 
            color: blue; 
            text-decoration: underline; 
            cursor: pointer; 
            font-size: 0.8em; 
            margin-top: 10px; 
            display: inline-block; 
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Memory Allocation Analyzer</h1>
        
        <div class="info">
            <p><strong>Instructions:</strong> Upload your <code>.speedscope.json</code> file to analyze allocation patterns.</p>
            <p>This tool identifies memory allocation hotspots without needing PerfView.</p>
        </div>
        
        <input type="file" id="fileInput" accept=".json">
        <div id="status"></div>
        
        <div id="results" style="margin-top: 20px; display: none;">
            <div id="summary"></div>
            <h2>Top Allocation Sources</h2>
            <div id="noData" style="display: none;">No allocation data found in the profile.</div>
            <table id="allocTable">
                <thead>
                    <tr>
                        <th>Function</th>
                        <th>Self Time (ms)</th>
                        <th>Total Time (ms)</th>
                        <th>% of Total</th>
                    </tr>
                </thead>
                <tbody></tbody>
            </table>
            <span class="debug-toggle" onclick="toggleDebug()">Show/Hide Debug Info</span>
            <div id="debugOutput"></div>
        </div>
    </div>

    <script>
        // Debug logging function
        function log(message, data) {
            const debugOutput = document.getElementById('debugOutput');
            const timestamp = new Date().toISOString().substr(11, 8);
            
            let logMessage = `[${timestamp}] ${message}`;
            if (data !== undefined) {
                if (typeof data === 'object') {
                    logMessage += ":\n" + JSON.stringify(data, null, 2).substring(0, 500);
                    if (JSON.stringify(data).length > 500) logMessage += "... (truncated)";
                } else {
                    logMessage += ": " + data;
                }
            }
            
            debugOutput.textContent += logMessage + "\n\n";
            // Auto-scroll to bottom
            debugOutput.scrollTop = debugOutput.scrollHeight;
            
            // Also log to console for additional debugging
            console.log(message, data);
        }
        
        function toggleDebug() {
            const debugOutput = document.getElementById('debugOutput');
            debugOutput.style.display = debugOutput.style.display === 'none' ? 'block' : 'none';
        }
        
        function showStatus(message, type) {
            const statusEl = document.getElementById('status');
            statusEl.innerHTML = `<div class="${type}">${message}</div>`;
        }
        
        document.getElementById('fileInput').addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (!file) return;
            
            // Clear previous results
            document.getElementById('debugOutput').textContent = '';
            document.getElementById('results').style.display = 'none';
            document.getElementById('summary').innerHTML = '';
            document.getElementById('allocTable').getElementsByTagName('tbody')[0].innerHTML = '';
            
            showStatus("Reading file...", "loading");
            log("File selected", { name: file.name, size: file.size });
            
            const reader = new FileReader();
            reader.onload = function(e) {
                try {
                    showStatus("Parsing JSON...", "loading");
                    log("File loaded, parsing JSON");
                    
                    const data = JSON.parse(e.target.result);
                    log("JSON parsed successfully", { 
                        keys: Object.keys(data),
                        hasProfiles: Boolean(data.profiles),
                        profileCount: data.profiles ? data.profiles.length : 0
                    });
                    
                    showStatus("Analyzing data...", "loading");
                    analyzeSpeedscopeData(data);
                    
                    showStatus("Analysis complete!", "success");
                    document.getElementById('results').style.display = 'block';
                } catch (err) {
                    log("Error processing file", { error: err.toString(), stack: err.stack });
                    showStatus(`Error: ${err.message}`, "error");
                }
            };
            
            reader.onerror = function(e) {
                log("File read error", e);
                showStatus("Error reading file", "error");
            };
            
            reader.readAsText(file);
        });
        
        function analyzeSpeedscopeData(data) {
            log("Starting analysis");
            
            // Basic validation
            if (!data.profiles || !data.profiles.length) {
                log("No profiles found in data", data);
                document.getElementById('summary').innerHTML = '<p class="error">No profile data found</p>';
                return;
            }
            
            // Try to handle different speedscope formats
            const profile = data.profiles[0];
            log("Selected profile", { 
                name: profile.name,
                type: profile.type,
                hasFrames: Boolean(profile.frames),
                hasSamples: Boolean(profile.samples),
                sampleCount: profile.samples ? profile.samples.length : 0
            });
            
            // Get frame lookup - handle both shared frames and profile-specific frames
            const frames = data.shared?.frames || profile.frames || [];
            log("Frames data", { 
                count: frames.length, 
                source: data.shared?.frames ? "shared" : "profile", 
                sample: frames.slice(0, 3) 
            });
            
            const frameLookup = {};
            frames.forEach((frame, index) => {
                frameLookup[index] = frame.name || `Frame ${index}`;
            });
            
            // Process data based on profile type
            let totalTime = 0;
            const functionStats = {};
            
            if (profile.type === 'evented') {
                log("Processing evented profile");
                // Handle evented profile format
                const events = profile.events || [];
                let openFrames = [];
                let lastTime = profile.startValue || 0;
                
                for (let i = 0; i < events.length; i++) {
                    const event = events[i];
                    const duration = event.at - lastTime;
                    
                    if (duration > 0 && openFrames.length > 0) {
                        // Add time to all open frames
                        totalTime += duration;
                        
                        for (let j = 0; j < openFrames.length; j++) {
                            const frameId = openFrames[j];
                            const frameName = frameLookup[frameId] || `Unknown Frame ${frameId}`;
                            
                            if (!functionStats[frameName]) {
                                functionStats[frameName] = { selfTime: 0, totalTime: 0 };
                            }
                            
                            // Only the top frame gets self time
                            if (j === openFrames.length - 1) {
                                functionStats[frameName].selfTime += duration;
                            }
                            
                            functionStats[frameName].totalTime += duration;
                        }
                    }
                    
                    lastTime = event.at;
                    
                    if (event.type === 'O') {  // Open frame
                        openFrames.push(event.frame);
                    } else if (event.type === 'C') {  // Close frame
                        openFrames.pop();
                    }
                }
            } else if (profile.samples && profile.weights) {
                log("Processing sampled profile");
                // Handle sampled profile format
                for (let i = 0; i < profile.samples.length; i++) {
                    const sample = profile.samples[i];
                    const weight = profile.weights[i];
                    totalTime += weight;
                    
                    // Credit the function at the top of the stack with this sample
                    if (sample.length > 0) {
                        const topFrameId = sample[sample.length - 1];
                        const frameName = frameLookup[topFrameId] || `Unknown Frame ${topFrameId}`;
                        
                        if (!functionStats[frameName]) {
                            functionStats[frameName] = { selfTime: 0, totalTime: 0 };
                        }
                        functionStats[frameName].selfTime += weight;
                        
                        // Credit all frames in the stack with total time
                        for (let j = 0; j < sample.length; j++) {
                            const frameId = sample[j];
                            const stackFrameName = frameLookup[frameId] || `Unknown Frame ${frameId}`;
                            
                            if (!functionStats[stackFrameName]) {
                                functionStats[stackFrameName] = { selfTime: 0, totalTime: 0 };
                            }
                            functionStats[stackFrameName].totalTime += weight;
                        }
                    }
                }
            } else {
                log("Unknown profile format, trying to analyze generically");
                // Try to handle other formats or provide useful information
                document.getElementById('summary').innerHTML = `
                    <p class="error">Unsupported speedscope format. Profile type: ${profile.type || 'unknown'}</p>
                    <p>This tool is designed for standard speedscope profile formats. Your file may be in a different format.</p>
                `;
                
                // Try to extract some basic info even if we don't understand the format
                const profileKeys = Object.keys(profile);
                document.getElementById('summary').innerHTML += `
                    <p>Profile contains these keys: ${profileKeys.join(', ')}</p>
                `;
                return;
            }
            
            log("Analysis complete", { 
                totalTime, 
                functionCount: Object.keys(functionStats).length 
            });
            
            // Prepare summary
            const summaryHTML = `
                <h2>Profile Summary</h2>
                <p><strong>Profile Name:</strong> ${profile.name || 'Unnamed Profile'}</p>
                <p><strong>Total Time:</strong> ${totalTime.toFixed(2)} ms</p>
                <p><strong>Total Functions:</strong> ${Object.keys(functionStats).length}</p>
            `;
            document.getElementById('summary').innerHTML = summaryHTML;
            
            // If we have no function data, show a message
            if (Object.keys(functionStats).length === 0) {
                document.getElementById('noData').style.display = 'block';
                document.getElementById('allocTable').style.display = 'none';
                return;
            }
            
            // Sort functions by self time
            const sortedFunctions = Object.entries(functionStats)
                .sort((a, b) => b[1].selfTime - a[1].selfTime)
                .slice(0, 50); // Show top 50
            
            // Display in table
            const tableBody = document.getElementById('allocTable').getElementsByTagName('tbody')[0];
            tableBody.innerHTML = '';
            
            sortedFunctions.forEach(([funcName, stats]) => {
                if (stats.selfTime > 0) {  // Only show functions with self time
                    const row = tableBody.insertRow();
                    
                    // Highlight allocation-related functions
                    const isAlloc = funcName.includes('Alloc') || funcName.includes('alloc') || 
                                    funcName.includes('new') || funcName.includes('New') ||
                                    funcName.includes('Create') || funcName.includes('Memory');
                    
                    if (isAlloc) {
                        row.classList.add('alloc-row');
                    }
                    
                    // Function name cell
                    const nameCell = row.insertCell(0);
                    nameCell.textContent = funcName;
                    
                    // Self time cell
                    const selfTimeCell = row.insertCell(1);
                    selfTimeCell.textContent = stats.selfTime.toFixed(2);
                    
                    // Total time cell
                    const totalTimeCell = row.insertCell(2);
                    totalTimeCell.textContent = stats.totalTime.toFixed(2);
                    
                    // Percentage cell
                    const pctCell = row.insertCell(3);
                    const pct = (stats.selfTime / totalTime * 100).toFixed(2);
                    pctCell.textContent = pct + '%';
                }
            });
        }
    </script>
</body>
</html>
EOL
        
        echo ""
        echo "Trace analysis completed! Results in $LOG_DIR/"
        echo ""
        echo "Heap Analysis Options:"
        echo "----------------------"
        echo "1. View allocation patterns in Speedscope:"
        echo "   Open https://www.speedscope.app and upload:"
        echo "   $TRACE_OUTPUT.speedscope.json"
        echo ""
        echo "2. Use the local allocation analyzer:"
        echo "   Open $LOG_DIR/allocation_analysis.html in your browser"
        echo "   Upload $TRACE_OUTPUT.speedscope.json when prompted"
        echo ""
        
        if [ -f "$LOG_DIR/initial_heap.gcdump" ] && [ -f "$LOG_DIR/final_heap.gcdump" ]; then
            echo "3. Compare heap snapshots:"
            echo "   - Initial: $LOG_DIR/initial_heap.gcdump"
            echo "   - Final: $LOG_DIR/final_heap.gcdump"
            echo "   - Basic size comparison in: $LOG_DIR/heap_comparison.txt"
            echo ""
            echo "   For detailed heap analysis:"
            echo "   - Use PerfView: Open PerfView > Memory > Diff Heap Snapshots"
            echo "   - Use Visual Studio: Debug > Performance Profiler > Memory Usage"
        fi
        
        echo ""
        echo "Understanding the results:"
        echo "-------------------------"
        echo "- Look for functions with high 'self time' in the allocation analyzer"
        echo "- Check for growth between initial and final heap snapshots"
        echo "- Focus on types with many instances or large total size"
        echo "- Note any unexpected retained objects that should have been collected"
    else
        echo "Warning: Trace files not found. Check for errors in $LOG_DIR/godot_trace_run.log"
    fi
else
    echo "Warning: Godot process exited during tracing."
fi

# Clean up any remaining processes
wait $TRACE_PID 2>/dev/null
wait $GODOT_PID 2>/dev/null

echo ""
echo "Tracing complete. Results saved to $LOG_DIR/"
echo "Done."
