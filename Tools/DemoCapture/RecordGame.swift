// macOS screen recorder used for the demo. Requires Screen Recording permission.
// Build: swiftc -parse-as-library Tools/DemoCapture/RecordGame.swift -o /tmp/dt-record
// Run: /tmp/dt-record /absolute/path/output.mp4 150 [stop-sentinel-path]
// Unity Tavern_Main window and maximized Game view must match the crop below.
// Captures 1080p/30 fps without audio; does not trigger the input sequence.
import Foundation
import ScreenCaptureKit
import AVFoundation
import AppKit
final class Delegate: NSObject, SCRecordingOutputDelegate {
 var started = false; var finished = false; var error: Error?
 func recordingOutputDidStartRecording(_ output: SCRecordingOutput) { started = true; print("RECORDING_STARTED"); fflush(stdout) }
 func recordingOutputDidFinishRecording(_ output: SCRecordingOutput) { finished = true }
 func recordingOutput(_ output: SCRecordingOutput, didFailWithError error: Error) { self.error=error; finished=true }
}
@main struct Main {
 static func main() async throws {
 _ = NSApplication.shared
 let args=CommandLine.arguments
 let content=try await SCShareableContent.excludingDesktopWindows(false,onScreenWindowsOnly:true)
 guard let win=content.windows.first(where:{$0.owningApplication?.bundleIdentifier == "com.unity3d.UnityEditor5.x" && ($0.title?.contains("Tavern_Main") ?? false)}) else {fatalError("Unity main window missing")}
 let filter=SCContentFilter(desktopIndependentWindow:win)
 print("window=\(win.windowID) frame=\(win.frame) contentRect=\(filter.contentRect) scale=\(filter.pointPixelScale)")
 let config=SCStreamConfiguration()
 let sx=win.frame.width/2560, sy=win.frame.height/1452
 config.sourceRect=CGRect(x:320*sx,y:274*sy,width:1920*sx,height:1080*sy)
 config.width=1920; config.height=1080
 config.minimumFrameInterval=CMTime(value:1,timescale:30)
 config.capturesAudio=false; config.captureMicrophone=false; config.showsCursor=false; config.ignoreShadowsSingleWindow=true
 let rc=SCRecordingOutputConfiguration();rc.outputURL=URL(fileURLWithPath:args[1]);rc.videoCodecType = .h264;rc.outputFileType = .mp4
 let delegate=Delegate();let output=SCRecordingOutput(configuration:rc,delegate:delegate)
 let stream=SCStream(filter:filter,configuration:config,delegate:nil)
 try stream.addRecordingOutput(output)
 try await stream.startCapture()
 for _ in 0..<100 {if delegate.started || delegate.finished {break};try await Task.sleep(nanoseconds:100_000_000)}
 guard delegate.started else {fatalError("Recording did not start")}
 let end=Date().addingTimeInterval(Double(args[2])!)
 while Date() < end {
 if args.count>3 && FileManager.default.fileExists(atPath:args[3]) {break}
 try await Task.sleep(nanoseconds:100_000_000)
 }
 try await stream.stopCapture()
 for _ in 0..<100 {if delegate.finished {break};try await Task.sleep(nanoseconds:100_000_000)}
 if let error=delegate.error {throw error}
 guard delegate.finished else {fatalError("Recording did not finish")}
 print("RECORDING_FINISHED \(output.recordedDuration.seconds)")
 }
}
